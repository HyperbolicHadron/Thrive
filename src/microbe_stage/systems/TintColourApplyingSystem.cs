﻿namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;

/// <summary>
///   Handles applying the shader "tint" parameter based on <see cref="ColourAnimation"/> to an
///   <see cref="EntityMaterial"/> that has microbe stage compatible shader parameter names
/// </summary>
/// <remarks>
///   <para>
///     This is marked as only reading the materials as this just applies a single shader parameter to the found
///     materials, but otherwise doesn't do anything to them.
///   </para>
/// </remarks>
[With(typeof(ColourAnimation))]
[With(typeof(EntityMaterial))]
[ReadsComponent(typeof(EntityMaterial))]
[RunsAfter(typeof(ColourAnimationSystem))]
[RuntimeCost(8)]
[RunsOnFrame]
[RunsOnMainThread]
public sealed class TintColourApplyingSystem : AEntitySetSystem<float>
{
    private readonly StringName tintName = new("tint");

    public TintColourApplyingSystem(World world) : base(world, null)
    {
    }

    public override void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var animation = ref entity.Get<ColourAnimation>();

        if (animation.ColourApplied)
            return;

        ref var entityMaterial = ref entity.Get<EntityMaterial>();

        if (entityMaterial.Materials == null)
            return;

        var materials = entityMaterial.Materials;

        var currentColour = animation.CurrentColour;

        if (animation.AnimateOnlyFirstMaterial)
        {
            if (materials.Length > 0)
            {
                materials[0].SetShaderParameter(tintName, currentColour);
            }
        }
        else
        {
            foreach (var material in materials)
            {
                material.SetShaderParameter(tintName, currentColour);
            }
        }

        animation.ColourApplied = true;
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            tintName.Dispose();
        }
    }
}
