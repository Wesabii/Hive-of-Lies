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
    [SerializeField] GameObject drawCostDisplay;
    [SerializeField] TMP_Text drawCostText;
    [SerializeField] GameObject drawsLeftDisplay;
    [SerializeField] TMP_Text drawsLeftText;
    [SerializeField] Button drawButton;
    [SerializeField] GameObject submitButton;
    [SerializeField] IntVariable favour;

    private int drawCost;
    private int drawsLeft;

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
        drawCostText.text = val.ToString();
        drawCost = val;
        drawButton.interactable = AbleToDraw();
    }

    public void EnableDrawCost(bool enabled)
    {
        drawCostDisplay.SetActive(enabled);
    }

    public void ChangeDrawsLeft(int val)
    {
        drawsLeftText.text = val.ToString();
        drawsLeft = val;
        drawButton.interactable = AbleToDraw();
    }

    public void EnableDrawsLeft(bool enabled)
    {
        drawsLeftDisplay.SetActive(enabled);
    }

    private bool AbleToDraw()
    {
        if (drawCost > favour) return false;
        if (drawsLeft <= 0) return false;
        return true;
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
