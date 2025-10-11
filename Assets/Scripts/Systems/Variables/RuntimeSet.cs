using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public abstract class RuntimeSet<T> : ScriptableObject
{
    public static implicit operator List<T>(RuntimeSet<T> a) => a.Value;

    [Tooltip("The initial value of this variable")]
    [SerializeField] private List<T> initialValue;

    [Tooltip("The current value of this variable")]
    [SerializeField] private List<T> currentValue;

    [Tooltip("The set that this set is a subset of")]
    [SerializeField] private RuntimeSet<T> subsetOf;

    [Tooltip("Whether this subset should start with the same value as the parent set")]
    [SerializeField] private bool startFull;

    [Tooltip("The set that this set is an extension of")]
    [SerializeField] private RuntimeSet<T> extensionOf;

    [Tooltip("Set of players that this set is a compliment of (requires sharing a subset)")]
    [SerializeField] private RuntimeSet<T> complement;

    [Space]
    [Space]

    [Tooltip("Should this variable be accessible on the server")]
    [SerializeField] private bool server;

    [Tooltip("Should this variable be accessible on clients")]
    [SerializeField] private bool client;

    [Tooltip("Whether this variable should persist through scene changes")]
    public bool Persistent = true;

#if UNITY_EDITOR
    [Space]
    [Space]

    [TextArea]
    [Tooltip("The tooltip for this variable. Doesn't have any mechanical purpose.")]
    [SerializeField] private string tooltip;
#endif

    /// <summary>
    /// The value of this variable
    /// </summary>
    public List<T> Value
    {
        get
        {
            return currentValue;
        }
        set
        {
            currentValue = value;
            if (value.Count == 0) AfterCleared?.Invoke();
        }
    }

    public int Count { get { return Value.Count; } }

    public T this[int key] { get { return Value[key]; } }

    public delegate void ItemDelegate(ref T item);

    /// <summary>
    /// Invoked before the item is added to allow manipulation
    /// </summary>
    public event ItemDelegate OnItemAdded;

    /// <summary>
    /// Invoked after the item is added
    /// </summary>
    public event System.Action<T> AfterItemAdded;

    /// <summary>
    /// Invoked before the item is removed to allow manipulation
    /// </summary>
    public event ItemDelegate OnItemRemoved;

    /// <summary>
    /// Invoked after the item is removed
    /// </summary>
    public event System.Action<T> AfterItemRemoved;

    public event System.Action AfterCleared;

    public void Add(T item) {
        if (Value.Contains(item)) return;

        OnItemAdded?.Invoke(ref item);

        //If the item was manipulated to become null, do not add that item
        if (item == null) return;
        currentValue.Add(item);
        AfterItemAdded?.Invoke(item);

        if (subsetOf != null) subsetOf.Add(item);
        if (complement != null) complement.Remove(item);
    }

    public void Remove(T item)
    {
        if (!Value.Contains(item)) return;

        OnItemRemoved?.Invoke(ref item);

        //If the item was manipulated to be null or something outside the set, do not remove that item
        if (item == null || !Value.Contains(item)) return;
        currentValue.Remove(item);
        AfterItemRemoved?.Invoke(item);

        if (extensionOf != null) extensionOf.Remove(item);
        if (complement != null) complement.Add(item);
    }

    public void RemoveAt(int index)
    {
        Value.RemoveAt(index);
    }

    public bool Contains(T val)
    {
        return Value.Contains(val);
    }

    public IEnumerator GetEnumerator()
    {
        return Value.GetEnumerator();
    }

    public void Shuffle()
    {
        Value.Shuffle();
    }

    public void OnEnable()
    {
        SetInitialValue();
        if (subsetOf == extensionOf && subsetOf != null) throw new System.Exception($"Set {name} cannot be a subset and an extension of the same set");
        if (subsetOf != null && subsetOf.extensionOf == this) throw new System.Exception($"{name} is a subset of {subsetOf}, and {subsetOf} is an extension of {name}");
        if (extensionOf != null && extensionOf.subsetOf == this) throw new System.Exception($"{name} is an extension of {extensionOf}, and {extensionOf} is a subset of {name}");

        if (subsetOf != null)
        {
            subsetOf.AfterCleared += () => subsetOf.AfterItemRemoved += item => Remove(item);
        }

        if (extensionOf != null)
        {
            extensionOf.AfterCleared += () => extensionOf.AfterItemAdded += item => Add(item);
        }
    }

    /// <summary>
    /// Resets the set back to the initial values
    /// </summary>
    public void ClearSet()
    {
        if (Persistent) return;

        SetInitialValue();

        if (AfterItemAdded != null)
        {
            foreach (System.Delegate d in AfterItemAdded.GetInvocationList())
            {
                AfterItemAdded -= (System.Action<T>)d;
            }
        }

        if (AfterItemRemoved != null) {
            foreach (System.Delegate d in AfterItemRemoved.GetInvocationList())
            {
                AfterItemRemoved -= (System.Action<T>)d;
            }
        }

        AfterCleared?.Invoke();
    }

    private void OnValidate()
    {
        //Set currentValue to bypass all the code that runs from setting Value
        //For some reason this isn't the same as just setting the current value to the initial value..
        if (!Application.isPlaying) return;
        
        SetInitialValue();
    }

    private void SetInitialValue()
    {
        currentValue = new();
        if (initialValue != null) currentValue.AddRange(initialValue);
        if (extensionOf != null) currentValue.AddRange(extensionOf.currentValue);
        if (subsetOf != null && startFull) currentValue.AddRange(subsetOf.currentValue);
        if (complement != null && subsetOf != null)
        {
            complement.Value = subsetOf.Value.FindAll((item) => !Value.Contains(item));
        }
    }
}
