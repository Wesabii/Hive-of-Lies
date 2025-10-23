using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Mirror;

public class GainUnusedRoles : RoleAbility
{
    [SerializeField] int rolesToGain = 2;

    #region SERVER
    [SerializeField] RoleDataSet rejectedRoles;
    [SerializeField] HivePlayerSet waspPlayers;

    /// <summary>
    /// The list of roles that we cannot take the ability of
    /// </summary>
    [SerializeField] List<RoleData> blacklist;

    [HideInInspector]
    public List<Role> Roles;

    string roleString = "";
    #endregion
    #region CLIENT
    [SerializeField] GameObject popup;
    [SerializeField] LocalizedString popupText;
    [SerializeField] LocalizedString targetText;
    [SerializeField] StringEvent changeTargetText;
    [SerializeField] StringEvent changeRoleText;
    #endregion    

    [Server]
    public void AfterAllRolesChosen()
    {
        rejectedRoles.Shuffle();
        int pickedRoles = 0;

        for (int i = rejectedRoles.Value.Count - 1; pickedRoles < rolesToGain && i >= 0; i--)
        {
            RoleData rl = rejectedRoles.Value[i];
            //Don't allow blacklisted roles
            if (blacklist.Contains(rl)) continue;
            //Make sure they're not given a role that has no ability
            if (rl.Abilities.Count == 0) continue;
            if (rl.Team == Team.Wasp) continue;
            GiveRole(rl);
            rejectedRoles.Remove(rl);
            pickedRoles++;
            roleString += rl.Description + "\n\n";
        }

        roleString = roleString.TrimEnd('\n');
        CreatePopup(roleString);

        foreach (HivePlayer ply in waspPlayers.Value)
        {
            if (ply.Target.Value == Owner) CreateTargetPopup(ply.connectionToClient, roleString, Owner.Role.Value.Data.RoleName);
            //If the player already has a target, then we know we can skip the event subscribing
            if (ply.Target.Value != null) return;
            ply.Target.AfterVariableChanged += (target) =>
            {
                if (target == Owner) CreateTargetPopup(ply.connectionToClient, roleString, Owner.Role.Value.Data.RoleName);
            };
        }
    }

    [TargetRpc]
    void CreatePopup(string abilities)
    {
        popup = Instantiate(popup);
        popup.GetComponent<Notification>().SetText(string.Format(popupText.GetLocalizedString(), abilities));
        changeRoleText.Invoke(abilities);
    }

    [TargetRpc]
    void CreateTargetPopup(NetworkConnection conn, string str, string twoBeesName)
    {
        popup = Instantiate(popup);
        popup.GetComponent<Notification>().SetText(string.Format(targetText.GetLocalizedString(), str));
        changeTargetText.Invoke(twoBeesName.ToUpper() + "\n" + str);
    }

    [Server]
    void GiveRole(RoleData data)
    {
        List<RoleAbility> abilities = new();
        for (int i = 0; i < data.Abilities.Count; i++)
        {
            GameObject abilityObject = Instantiate(data.Abilities[i]);
            NetworkServer.Spawn(abilityObject, Owner.connectionToClient);

            RoleAbility[] scripts = abilityObject.GetComponents<RoleAbility>();

            foreach (RoleAbility ability in scripts)
            {
                ability.Owner = Owner;
                abilities.Add(ability);
            }
        }

        Role role = new()
        {
            Abilities = abilities,
            Data = data
        };

        Roles.Add(role);
    }
}
