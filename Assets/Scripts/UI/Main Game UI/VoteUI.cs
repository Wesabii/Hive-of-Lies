using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Steamworks;
using UnityEngine.UI;
using System;

public class VoteUI : MonoBehaviour
{
    #region CLIENT
    [SerializeField] TMP_Text voteNumber;
    [SerializeField] TMP_Text yesCost;
    [SerializeField] TMP_Text noCost;
    [SerializeField] Transform submitThumb;

    [SerializeField] Button yesVote;
    [SerializeField] Button noVote;
    [SerializeField] Button submitButton;

    [SerializeField] GameObject voteUI;

    private int upvoteCost;
    private int downvoteCost;
    private int numVotes;

    public delegate int VoteCostCalculation(bool isUpvote, int numVotes);
    private VoteCostCalculation getVoteCost;

    public event Action<int> OnLockInVote;

    public delegate void ModifyVotes(ref int numVotes);
    public event ModifyVotes OnVoteChange;

    [Tooltip("The amount of favour the local player has")]
    [SerializeField] IntVariable favour;

    [Tooltip("Whether this player is alive")]
    [SerializeField] BoolVariable alive;
    #endregion

    /// <summary>
    /// Called when the vote starts
    /// </summary>
    /// <param name="msg"></param>
    public void VoteStarted()
    {
        if (!alive) return;
        voteUI.SetActive(true);
        numVotes = 0;
    }

    /// <summary>
    /// Called when a player increases their vote (upvotes)
    /// </summary>
    public void IncreaseVote()
    {
        numVotes++;
        favour.Value -= upvoteCost;

        ChangeVote();
    }

    /// <summary>
    /// Called when a player deceases their vote (downvotes)
    /// </summary>
    public void DecreaseVote()
    {
        numVotes--;
        favour.Value -= downvoteCost;

        ChangeVote();
    }

    private void ChangeVote()
    {
        upvoteCost = getVoteCost(true, numVotes);
        downvoteCost = getVoteCost(false, numVotes);

        int totalVotes = numVotes;
        OnVoteChange?.Invoke(ref totalVotes);
        voteNumber.text = Mathf.Abs(totalVotes).ToString();

        submitThumb.localScale = new Vector3(1, (totalVotes >= 0 ? 1f : -1f), 1);
        submitButton.interactable = numVotes != 0;

        noCost.text = downvoteCost < 0 ? $"+{-downvoteCost}" : $"{downvoteCost}";
        noVote.interactable = downvoteCost <= favour || downvoteCost <= 0;

        yesCost.text = upvoteCost < 0 ? $"+{-upvoteCost}" : $"{upvoteCost}";
        yesVote.interactable = upvoteCost <= favour || upvoteCost <= 0;
    }

    public void LockInVote()
    {
        voteUI.SetActive(false);
        OnLockInVote?.Invoke(numVotes);
    }

    public void SetVoteCostCalculation(VoteCostCalculation calc)
    {
        getVoteCost = calc;
    }
}
