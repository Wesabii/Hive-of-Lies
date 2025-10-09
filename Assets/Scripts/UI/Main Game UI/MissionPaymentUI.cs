using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionPaymentUI : MonoBehaviour
{
    [Tooltip("The amount of favour the local player has")]
    [SerializeField] IntVariable favour;
    [SerializeField] IntVariable favourContribution;
    private int favourContributionCost;

    [SerializeField] TMPro.TMP_Text contributionText;
    [SerializeField] Button increaseCostButton;
    [SerializeField] Button decreaseCostButton;
    [SerializeField] GameObject UI;

    public event Action<int> OnPaid;

    public void Setup(int fairShare)
    {
        UI.SetActive(true);
        //Pass the mission cost into this so each player starts by providing a fair share of the mission cost
        int startingContribution = Mathf.Min(favour.Value, fairShare);
        contributionText.text = startingContribution.ToString();
        favourContributionCost = fairShare;
        favourContribution.Value = fairShare;

        if (startingContribution == favour.Value)
        {
            increaseCostButton.interactable = false;
        }

    }

    public void IncreaseContribution()
    {
        favourContributionCost++;
        favourContribution.Value++;
        decreaseCostButton.interactable = true;
        if (favourContributionCost >= favour.Value)
        {
            increaseCostButton.interactable = false;
        }
    }

    public void DecreaseContribution()
    {
        favourContributionCost--;
        favourContribution.Value--;
        increaseCostButton.interactable = true;
        if (favourContributionCost == 0)
        {
            decreaseCostButton.interactable = false;
        }
    }

    public void PayCost()
    {
        OnPaid?.Invoke(favourContributionCost);
        UI.SetActive(false);
    }
}
