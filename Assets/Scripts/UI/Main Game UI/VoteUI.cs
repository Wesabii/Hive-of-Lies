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

    private int _upvoteCost;
    private int upvoteCost
    { 
        get { return _upvoteCost; }
        set
        {
            _upvoteCost = value;
            yesCost.text = value < 0 ? $"+{-value}" : $"{value}";
            yesVote.interactable = value <= favour || value <= 0;
        }
    }
    private int _downvoteCost;
    private int downvoteCost
    {
        get { return _downvoteCost; }
        set
        {
            _downvoteCost = value;
            noCost.text = value < 0 ? $"+{-value}" : $"{value}";
            noVote.interactable = value <= favour || value <= 0;
        }
    }
    private int _numVotes;
    private int numVotes
    {
        get { return _numVotes; }
        set
        {
            _numVotes = value;
            int totalVotes = numVotes;
            OnVoteChange?.Invoke(ref totalVotes);
            voteNumber.text = Mathf.Abs(totalVotes).ToString();

            submitThumb.localScale = new Vector3(1, (totalVotes >= 0 ? 1f : -1f), 1);
            submitButton.interactable = numVotes != 0;
        }
    }

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

    public void VoteStarted()
    {
        if (!alive) return;
        numVotes = 0;
        upvoteCost = getVoteCost(true, 0);
        downvoteCost = getVoteCost(false, 0);
        voteUI.SetActive(true);
    }

    public void ChangeVote(bool upvote)
    {
        numVotes += upvote ? 1 : -1;
        favour.Value -= upvote ? upvoteCost : downvoteCost;
        upvoteCost = getVoteCost(true, numVotes);
        downvoteCost = getVoteCost(false, numVotes);
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
