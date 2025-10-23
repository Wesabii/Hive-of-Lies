using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuyRedrawsUI : MonoBehaviour
{
    [Tooltip("The amount of favour the local player has")]
    [SerializeField] IntVariable favour;
    [SerializeField] IntVariable nextRedrawCost;
    private int redraws;

    [SerializeField] TMPro.TMP_Text redrawsText;
    [SerializeField] TMPro.TMP_Text drawCostText;
    [SerializeField] Button addDrawButton;
    [SerializeField] GameObject UI;

    public event Action<int> OnAddDraw;
    public event Action<int> OnContinue;

    private void Start()
    {
        nextRedrawCost.AfterVariableChanged += (val) => drawCostText.text = val.ToString();
    }

    public void Setup(int cost)
    {
        UI.SetActive(true);
        redraws = 0;
        redrawsText.text = "0";
        drawCostText.text = cost.ToString();
        nextRedrawCost.Value = cost;

        addDrawButton.interactable = favour.Value >= nextRedrawCost.Value;
    }

    public void AddDraw()
    {
        favour.Value -= nextRedrawCost.Value;
        OnAddDraw?.Invoke(redraws);
        redraws++;
        redrawsText.text = redraws.ToString();
        if (nextRedrawCost.Value > favour.Value)
        {
            addDrawButton.interactable = false;
        }
    }

    public void PayCost()
    {
        OnContinue?.Invoke(redraws);
        UI.SetActive(false);
    }
}
