using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class VictoryPointBar : NetworkBehaviour
{
    [SerializeField] Transform waspFactBar;
    [SerializeField] Transform honeyStolenBar;

    [SerializeField] Sprite segmentSprite;
    [SerializeField] Color waspFactSegmentColour;
    [SerializeField] Color honeyStolenSegmentColour;

    [Tooltip("How many wasp facts are needed for the Bees to win")]
    [SerializeField] IntVariable waspFactsNeeded;

    [Tooltip("How much honey stolen is needed for the Wasps to win")]
    [SerializeField] IntVariable honeyStolenNeeded;

    [SyncVar(hook = nameof(BeePointsRequiredChanged))] int wfNeeded;
    [SyncVar(hook = nameof(WaspPointsRequiredChanged))] int hsNeeded;

    [SerializeField] List<MissionEffect> beePointEffects;
    [SerializeField] List<MissionEffect> waspPointEffects;
    [SerializeField] IntVariable waspPoints;
    [SerializeField] IntVariable beePoints;

    public override void OnStartServer()
    {
        //Set these now so the client can grab them immediately
        wfNeeded = waspFactsNeeded;
        hsNeeded = honeyStolenNeeded;

        waspPoints.AfterVariableChanged += (val) => AfterPointGained(waspPointEffects, val);
        beePoints.AfterVariableChanged += (val) => AfterPointGained(beePointEffects, val);

        waspFactsNeeded.AfterVariableChanged += (val) => wfNeeded = val;
        honeyStolenNeeded.AfterVariableChanged += (val) => hsNeeded = val;
    }

    [Client]
    private void BeePointsRequiredChanged(int oldVal, int newVal)
    {
        CreateSegments(newVal, waspFactBar, waspFactSegmentColour);
    }

    [Client]
    private void WaspPointsRequiredChanged(int oldVal, int newVal)
    {
        CreateSegments(newVal, honeyStolenBar, honeyStolenSegmentColour);
    }

    [Client]
    void CreateSegments(int num, Transform parent, Color colour)
    {

        for (int i = 2; i < parent.childCount; i++)
        {
            Destroy(parent.GetChild(i).gameObject);
        }

        for (int i = 0; i < num; i++)
        {
            GameObject segment = new GameObject("Segment");
            Image image = segment.AddComponent<Image>();
            image.sprite = segmentSprite;
            image.color = colour;
            image.type = Image.Type.Sliced;

            segment.transform.SetParent(parent);
        }
    }

    [Server]
    void AfterPointGained(List<MissionEffect> effectList, int num)
    {
        if (effectList.Count < num) return;

        MissionEffect eff = effectList[num - 1];

        if (eff == null) return;

        eff.TriggerEffect();
    }
}
