using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuyRedrawsUI : MonoBehaviour
{
    [Tooltip("The amount of favour the local player has")]
    [SerializeField] IntVariable favour;
    [SerializeField] IntVariable redrawCost;
    private int redraws;

    [SerializeField] TMPro.TMP_Text redrawsText;
    [SerializeField] TMPro.TMP_Text drawCostText;
    [SerializeField] Button addDrawButton;
    [SerializeField] GameObject UI;

    public event Action<int> OnAddDraw;
    public event Action<int> OnContinue;

    private void Start()
    {
        redrawCost.AfterVariableChanged += (val) => drawCostText.text = val.ToString();
    }

    public void Setup(int cost)
    {
        UI.SetActive(true);
        redrawsText.text = "0";
        drawCostText.text = cost.ToString();
        redrawCost.Value = cost;

        if (redrawCost > favour.Value)
        {
            addDrawButton.interactable = false;
        }
    }

    public void AddDraw()
    {
        redraws++;
        redrawsText.text = redraws.ToString();
        favour.Value -= redrawCost.Value;
        if (redrawCost.Value > favour.Value)
        {
            addDrawButton.interactable = false;
        }
        OnAddDraw?.Invoke(redraws);
    }

    public void PayCost()
    {
        OnContinue?.Invoke(redraws);
        UI.SetActive(false);
    }
}
