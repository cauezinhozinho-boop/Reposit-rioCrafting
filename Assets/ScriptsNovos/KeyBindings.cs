using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameAction
{
    MoveForward,
    MoveBack,
    MoveLeft,
    MoveRight,
    Run,
    Jump,
    Interact,
    Inventory,
    Flashlight,
    MinigameLeft,
    MinigameRight
}

[Serializable]
public struct KeyBinding
{
    public bool isMouse;
    public KeyCode keyboard;
    public int mouseButton;

    public static KeyBinding FromKey(KeyCode key)
        => new KeyBinding { isMouse = false, keyboard = key };

    public static KeyBinding FromMouse(int button)
        => new KeyBinding { isMouse = true, mouseButton = button };

    public bool IsDown()
        => isMouse ? Input.GetMouseButtonDown(mouseButton) : Input.GetKeyDown(keyboard);

    public bool IsHeld()
        => isMouse ? Input.GetMouseButton(mouseButton) : Input.GetKey(keyboard);

    public bool IsUp()
        => isMouse ? Input.GetMouseButtonUp(mouseButton) : Input.GetKeyUp(keyboard);

    public string DisplayName()
    {
        if (isMouse)
        {
            switch (mouseButton)
            {
                case 0: return "Mouse Esq.";
                case 1: return "Mouse Dir.";
                case 2: return "Mouse Meio";
                default: return "Mouse " + mouseButton;
            }
        }
        return PrettyKeyName(keyboard);
    }

    static string PrettyKeyName(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.LeftShift: return "Shift Esq.";
            case KeyCode.RightShift: return "Shift Dir.";
            case KeyCode.LeftControl: return "Ctrl Esq.";
            case KeyCode.RightControl: return "Ctrl Dir.";
            case KeyCode.LeftAlt: return "Alt Esq.";
            case KeyCode.RightAlt: return "Alt Dir.";
            case KeyCode.Space: return "Espaço";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Return: return "Enter";
            case KeyCode.UpArrow: return "Seta Cima";
            case KeyCode.DownArrow: return "Seta Baixo";
            case KeyCode.LeftArrow: return "Seta Esq.";
            case KeyCode.RightArrow: return "Seta Dir.";
            default: return k.ToString();
        }
    }
}

public static class KeyBindings
{
    public static event Action<GameAction> OnBindingChanged;
    public static event Action<GameAction> OnRebindStarted;
    public static event Action<GameAction> OnRebindCancelled;

    static Dictionary<GameAction, KeyBinding> bindings;

    static readonly Dictionary<GameAction, KeyBinding> defaults =
        new Dictionary<GameAction, KeyBinding>
    {
        { GameAction.MoveForward,   KeyBinding.FromKey(KeyCode.W) },
        { GameAction.MoveBack,      KeyBinding.FromKey(KeyCode.S) },
        { GameAction.MoveLeft,      KeyBinding.FromKey(KeyCode.A) },
        { GameAction.MoveRight,     KeyBinding.FromKey(KeyCode.D) },
        { GameAction.Run,           KeyBinding.FromKey(KeyCode.LeftShift) },
        { GameAction.Jump,          KeyBinding.FromKey(KeyCode.Space) },
        { GameAction.Interact,      KeyBinding.FromKey(KeyCode.E) },
        { GameAction.Inventory,     KeyBinding.FromKey(KeyCode.I) },
        { GameAction.Flashlight,    KeyBinding.FromKey(KeyCode.F) },
        { GameAction.MinigameLeft,  KeyBinding.FromMouse(0) },
        { GameAction.MinigameRight, KeyBinding.FromMouse(1) },
    };

    static GameAction? rebinding;
    static int rebindStartFrame = -1;
    static KeyBindingsRunner runner;

    public static bool IsRebinding => rebinding.HasValue;
    public static GameAction RebindingAction => rebinding ?? default;

    static KeyBindings() { Load(); }

    public static KeyBinding Get(GameAction action)
    {
        if (bindings == null) Load();
        return bindings[action];
    }

    public static bool GetDown(GameAction action) => Get(action).IsDown();
    public static bool GetHeld(GameAction action) => Get(action).IsHeld();
    public static bool GetUp(GameAction action) => Get(action).IsUp();

    // ============================
    // Rebind
    // ============================

    public static void StartRebind(GameAction action)
    {
        rebinding = action;
        rebindStartFrame = Time.frameCount;
        EnsureRunner();
        OnRebindStarted?.Invoke(action);
    }

    public static void CancelRebind()
    {
        if (!rebinding.HasValue) return;
        var a = rebinding.Value;
        rebinding = null;
        OnRebindCancelled?.Invoke(a);
    }

    static void CompleteRebind(KeyBinding newBinding)
    {
        if (!rebinding.HasValue) return;

        var action = rebinding.Value;

        // =========================================================
        // SWAP: se a tecla nova já pertence a outra ação,
        // a outra ação recebe a tecla antiga desta.
        // =========================================================
        var previous = bindings[action];
        GameAction? conflictAction = FindActionUsing(newBinding, action);

        if (conflictAction.HasValue)
        {
            bindings[conflictAction.Value] = previous;
        }

        bindings[action] = newBinding;
        rebinding = null;

        Save();

        OnBindingChanged?.Invoke(action);
        if (conflictAction.HasValue)
            OnBindingChanged?.Invoke(conflictAction.Value);
    }

    /// <summary>
    /// Procura qual ação (que não seja a 'exclude') está usando a tecla dada.
    /// Retorna null se ninguém usa.
    /// </summary>
    static GameAction? FindActionUsing(KeyBinding target, GameAction exclude)
    {
        foreach (var kv in bindings)
        {
            if (kv.Key == exclude) continue;

            KeyBinding b = kv.Value;

            if (b.isMouse != target.isMouse) continue;

            if (b.isMouse && b.mouseButton == target.mouseButton)
                return kv.Key;

            if (!b.isMouse && b.keyboard == target.keyboard)
                return kv.Key;
        }

        return null;
    }

    public static void ResetToDefaults()
    {
        bindings = new Dictionary<GameAction, KeyBinding>(defaults);
        Save();

        foreach (var a in defaults.Keys)
            OnBindingChanged?.Invoke(a);
    }

    // ============================
    // Persistência
    // ============================

    static string PrefKey(GameAction a) => "bind_" + a;

    static void Save()
    {
        foreach (var kv in bindings)
        {
            KeyBinding b = kv.Value;
            string v = b.isMouse
                ? ("m" + b.mouseButton)
                : ((int)b.keyboard).ToString();

            PlayerPrefs.SetString(PrefKey(kv.Key), v);
        }
        PlayerPrefs.Save();
    }

    static void Load()
    {
        bindings = new Dictionary<GameAction, KeyBinding>();

        foreach (var kv in defaults)
        {
            string saved = PlayerPrefs.GetString(PrefKey(kv.Key), "");

            if (string.IsNullOrEmpty(saved))
            {
                bindings[kv.Key] = kv.Value;
                continue;
            }

            if (saved[0] == 'm')
            {
                bindings[kv.Key] = int.TryParse(saved.Substring(1), out int mb)
                    ? KeyBinding.FromMouse(mb)
                    : kv.Value;
            }
            else
            {
                bindings[kv.Key] = int.TryParse(saved, out int kc)
                    ? KeyBinding.FromKey((KeyCode)kc)
                    : kv.Value;
            }
        }
    }

    // ============================
    // Runner (só escuta enquanto rebindando)
    // ============================

    static void EnsureRunner()
    {
        if (runner != null) return;

        var go = new GameObject("[KeyBindings]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<KeyBindingsRunner>();
    }

    internal static void ProcessRebindInput()
    {
        if (!rebinding.HasValue) return;

        // Ignora o frame do clique que abriu o rebind
        if (Time.frameCount == rebindStartFrame) return;

        // Esc cancela
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            return;
        }

        // Mouse
        for (int i = 0; i < 3; i++)
        {
            if (Input.GetMouseButtonDown(i))
            {
                CompleteRebind(KeyBinding.FromMouse(i));
                return;
            }
        }

        // Teclado
        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
        {
            if (kc == KeyCode.None) continue;
            if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6) continue;

            if (Input.GetKeyDown(kc))
            {
                CompleteRebind(KeyBinding.FromKey(kc));
                return;
            }
        }
    }
}

public class KeyBindingsRunner : MonoBehaviour
{
    void Update() => KeyBindings.ProcessRebindInput();
}