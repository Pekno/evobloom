using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

public partial class AnimationComponent : BloomyComponent
{
    [Export] public AnimationTree tree;

    [Export] public AnimationPlayer animationPlayer;
    private Dictionary<string, (Type, Func<IBloomyComponent, bool>)> _dic = new Dictionary<string, (Type, Func<IBloomyComponent, bool>)>{
        {"is_walking", (typeof(MuscleComponent), (x) => x is MuscleComponent m && m.CurrentVelocity.Length() > 0.1f)},
        {"is_idle", (typeof(MuscleComponent), (x) => x is MuscleComponent m && m.CurrentVelocity.Length() <= 0.1f)},
    };
    private Dictionary<Type, IBloomyComponent> _components = new Dictionary<Type, IBloomyComponent>();

    [ExportGroup("Visual Scaling (Life Stage)")]
    [Export] public float ScaleTransitionDuration = 1.0f;
    [Export] public NodePath VisualsRootPath { get; set; }

    private Tween _scaleTween;
    private GrowingComponent _growing;
    private Node2D _visualsRoot;


    public override void _Ready()
    {
        // reflection only once, for each distinct Type
        var getBodyPartMethod = typeof(BloomyComponent)
            .GetMethod(nameof(GetBodyPart),
                       BindingFlags.Instance | BindingFlags.Public);
        if (getBodyPartMethod == null)
            throw new InvalidOperationException("Could not find GetBodyPart<T>()");

        foreach (var (param, info) in _dic)
        {
            var (type, _) = info;
            if (_components.ContainsKey(type))
                continue;

            var generic = getBodyPartMethod.MakeGenericMethod(type);
            var comp = (IBloomyComponent)generic.Invoke(this, null);
            _components.Add(type, comp);
        }

        _growing = GetBodyPart<GrowingComponent>();
        if (_growing != null)
        {
            _growing.LifeStageChanged += OnLifeStageChanged;
            // Call explicitly to set initial scale.
            // Pass current stage for both new and old to ensure it runs.
            OnLifeStageChanged(_growing.CurrentLifeStage, _growing.CurrentLifeStage);
        }
        else
        {
            GD.PrintErr($"*{_bloomy?.Surname}* AnimationComponent: GrowingComponent not found! Visual scaling will not occur.");
        }

        if (VisualsRootPath != null && !VisualsRootPath.IsEmpty)
        {
            _visualsRoot = GetNodeOrNull<Node2D>(VisualsRootPath);
            if (_visualsRoot == null)
            {
                GD.PrintErr($"*{_bloomy?.Surname}* AnimationComponent: VisualsRootPath '{VisualsRootPath}' not found. Defaulting to scaling Bloomy root.");
                _visualsRoot = _bloomy;
            }
        }
        else
        {
            _visualsRoot = _bloomy;
        }
    }

    public override void ProcessComponent(float delta)
    {
        if (tree == null) return;
        foreach (var (key, value) in _dic)
        {
            var (type, func) = value;
            if (!_components.TryGetValue(type, out IBloomyComponent component) || component == null)
                continue;

            bool result = func(component);
            tree.Set("parameters/conditions/" + key, result);
        }

        var muscle = GetBodyPart<MuscleComponent>();
        if (muscle != null)
        {
            tree.Set("parameters/Idle/blend_position", muscle.CurrentVelocity.Normalized());
            tree.Set("parameters/Walking/blend_position", muscle.CurrentVelocity.Normalized());
        }
    }

    private void OnLifeStageChanged(LifeStage newStage, LifeStage oldStage)
    {
        if (_visualsRoot == null || _growing == null) // Add _growing check for safety
        {
            if (Debug && _bloomy != null) GD.Print($"*{_bloomy.Surname}* AnimationComponent: VisualsRoot or GrowingComponent not set, cannot apply scale for stage {newStage}.");
            return;
        }

        Vector2 targetScale = _growing.GetCurrentVisualScale();

        _scaleTween?.Kill();

        _scaleTween = CreateTween();
        _scaleTween.SetTrans(Tween.TransitionType.Sine);
        _scaleTween.SetEase(Tween.EaseType.InOut);
        _scaleTween.TweenProperty(_visualsRoot, "scale", targetScale, ScaleTransitionDuration);

        if (Debug && _bloomy != null) GD.Print($"*{_bloomy.Surname}* AnimationComponent: Scaling to {targetScale} for stage {newStage}.");
    }

    public override void _ExitTree()
    {
        if (_growing != null)
        {
            _growing.LifeStageChanged -= OnLifeStageChanged;
        }
        base._ExitTree();
    }
}
