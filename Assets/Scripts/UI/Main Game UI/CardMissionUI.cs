using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;
using System;

public class CardMissionUI : MonoBehaviour
{
    #region CLIENT

    [SerializeField] GameObject UI;
    [SerializeField] Image drawResult;
    [SerializeField] Image nextCard;
    [SerializeField] TMP_Text drawCost;
    [SerializeField] Button drawButton;
    [SerializeField] GameObject submitButton;
    [SerializeField] IntVariable favour;

    [Tooltip("Returns true if the player is on the mission")]
    [SerializeField] BoolVariable isOnMission;

    public event Action onDraw;
    public event Action onPlay;
    #endregion

    public void ShowUI()
    {
        if (!isOnMission) return;
        UI.SetActive(true);
    }

    public void HideUI()
    {
        UI.SetActive(false);
    }

    public void ChangeHandCard(Sprite sprite)
    {
        drawResult.color = new Color(drawResult.color.r, drawResult.color.g, drawResult.color.b, 1);
        drawResult.sprite = sprite;
    }

    public void ChangeTopCard(Sprite sprite)
    {
        nextCard.gameObject.SetActive(true);
        nextCard.sprite = sprite;
    }

    public void ShowTopCard(bool show)
    {
        nextCard.gameObject.SetActive(show);
    }

    public void ChangeDrawCost(int val)
    {
        drawCost.text = val.ToString();
        drawButton.interactable = val <= favour;
    }

    public void DrawCard()
    {
        onDraw?.Invoke();
    }

    public void PlayCard()
    {
        UI.SetActive(false);
        drawResult.color = new Color(drawResult.color.r, drawResult.color.g, drawResult.color.b, 0);
        onPlay?.Invoke();
    }
}
