using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Information about the role
/// </summary>
[CreateAssetMenu(fileName = "RoleData", menuName = "Roles/Create role")]
public class RoleData : ScriptableObject
{
    /// <summary>
    /// Private counterpart to <see cref="RoleName"/>
    /// </summary>
    [SerializeField] LocalizedString roleName;

    /// <summary>
    /// Name of the role
    /// </summary>
    public string RoleName
    {
        get
        {
            return roleName.GetLocalizedString();
        }
    }

    /// <summary>
    /// Private counterpart to <see cref="Description"/>
    /// </summary>
    [SerializeField] LocalizedString description;

    /// <summary>
    /// Description for the role
    /// </summary>
    public string Description
    {
        get
        {
            return description.GetLocalizedString();
        }
    }

    /// <summary>
    /// The team the role belongs to
    /// </summary>
    [field: SerializeField]
    public Team Team { get; private set; }

    /// <summary>
    /// Amount of influence the role starts with
    /// </summary>
    [field: SerializeField]
    public int StartingFavour { get; private set; } = 10;

    /// <summary>
    /// The logic for the roles ability
    /// </summary>
    [field: SerializeField]
    public List<GameObject> Abilities { get; private set; }

    /// <summary>
    /// The minimum number of players required in the lobby for this role to appear
    /// </summary>
    [field: SerializeField]
    public int PlayersRequired { get; private set; }

    /// <summary>
    /// Sprite used for the role UI
    /// </summary>
    [field: SerializeField]
    public Sprite Sprite { get; private set; }

    [field: SerializeField]
    public Difficulty Difficulty;

    /// <summary>
    /// Whether the role should appear in games
    /// </summary>
    [field: SerializeField]
    public bool Enabled { get; private set; } = true;

    [SerializeField] LocalizedString targetHint;

    /// <summary>
    /// What you should look for if this role is your target
    /// </summary>
    public string TargetHint
    {
        get
        {
            return targetHint.GetLocalizedString();
        }
    }
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}