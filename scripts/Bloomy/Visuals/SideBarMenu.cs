using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class SideBarMenu : Control // Or PanelContainer, etc.
{
    [Export] public PackedScene FeelingUIPieceScene { get; private set; } // Assign your Sidebar_menu_feeling.tscn
    [Export] public NodePath FeelingsContainerPath { get; private set; } // Path to your VBoxContainer

    private VBoxContainer _feelingsContainer;
    private Bloomy _currentTargetBloomy;
    private BrainComponent _targetBrain;

    // Store references to the instantiated UI pieces to update them
    private List<SidebarMenuFeeling> _feelingUIPieces = new List<SidebarMenuFeeling>();

    public override void _Ready()
    {
        if (FeelingUIPieceScene == null)
        {
            GD.PrintErr("SideBarMenu: FeelingUIPieceScene not assigned!");
            Visible = false;
            return;
        }

        _feelingsContainer = GetNodeOrNull<VBoxContainer>(FeelingsContainerPath);
        if (_feelingsContainer == null)
        {
            GD.PrintErr($"SideBarMenu: FeelingsContainer not found at path: {FeelingsContainerPath}");
            Visible = false;
            return;
        }
        Visible = false; // Initially hidden
    }

    public void ShowMenuForBloomy(Bloomy bloomy)
    {
        if (bloomy == null)
        {
            HideMenu();
            return;
        }

        _currentTargetBloomy = bloomy;
        _targetBrain = _currentTargetBloomy.GetBodyPart<BrainComponent>();

        if (_targetBrain == null)
        {
            GD.PrintErr("SideBarMenu: Selected Bloomy does not have a BrainComponent.");
            HideMenu();
            return;
        }

        PopulateFeelingsUI();
        Visible = true;
        // Optional: Bring to front if it's part of a larger UI
        // MoveToFront();
    }

    public void HideMenu()
    {
        Visible = false;
        _currentTargetBloomy = null;
        _targetBrain = null;
        ClearFeelingsUI();
    }

    private void ClearFeelingsUI()
    {
        foreach (var child in _feelingsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _feelingUIPieces.Clear();
    }

    private void PopulateFeelingsUI()
    {
        ClearFeelingsUI();

        if (_targetBrain == null) return;

        List<Feeling> feelings = _targetBrain.GetFeelingsList(); // Assuming this method exists
        if (feelings == null || !feelings.Any())
        {
            // Optional: Add a label saying "No feelings data"
            return;
        }

        // Sort feelings alphabetically for consistent order, or by another criteria
        foreach (var feeling in feelings.OrderBy(f => f.Type.ToString()))
        {
            SidebarMenuFeeling uiPieceInstance = FeelingUIPieceScene.Instantiate<SidebarMenuFeeling>();
            _feelingsContainer.AddChild(uiPieceInstance);
            // Assuming MaxFeelingWeightDisplay is defined somewhere accessible, e.g., a const or from config
            // For now, let's use a default of 1.0f for the slider's max value.
            // You might want to pass the brain's MaxFeelingWeightDisplay if it has one, or a global constant.
            float maxDisplayWeight = 1.0f; // Or get this from brain/config
            uiPieceInstance.Initialize(feeling.Type, feeling.Weight, maxDisplayWeight);
            _feelingUIPieces.Add(uiPieceInstance);
        }
        UpdateFeelingValues(); // To set initial highlights
    }

    public override void _Process(double delta)
    {
        if (Visible && _targetBrain != null && _feelingUIPieces.Any())
        {
            UpdateFeelingValues();
        }
    }

    private void UpdateFeelingValues()
    {
        if (_targetBrain == null) return;

        FeelingType activeFeeling = _targetBrain.LastEmittedFeeling; // Assuming this property exists

        // It's possible the _feelings list in BrainComponent gets re-created or modified.
        // For simplicity, we assume the list of Feeling objects themselves are stable references
        // and only their .Weight property changes. If not, we might need to re-fetch the list.
        List<Feeling> currentFeelings = _targetBrain.GetFeelingsList();

        foreach (SidebarMenuFeeling uiPiece in _feelingUIPieces)
        {
            Feeling correspondingFeeling = currentFeelings.FirstOrDefault(f => f.Type == uiPiece.GetFeelingType());
            if (correspondingFeeling != null)
            {
                uiPiece.UpdateValue(correspondingFeeling.Weight);
                uiPiece.Highlight(correspondingFeeling.Type == activeFeeling);
            }
            else
            {
                // This feeling type is no longer in the brain's list? Hide or remove this UI piece.
                // For now, let's assume this doesn't happen frequently.
                // uiPiece.Visible = false;
            }
        }
    }

    // Call this method when a Bloomy is clicked
    // You'll need a way to get this signal, e.g., from CursorController or a global event bus
    public void OnBloomySelected(Bloomy bloomy) // Example signature
    {
        if (Visible && _currentTargetBloomy == bloomy)
        {
            HideMenu(); // Toggle off if clicking the same bloomy
        }
        else
        {
            ShowMenuForBloomy(bloomy);
        }
    }
}