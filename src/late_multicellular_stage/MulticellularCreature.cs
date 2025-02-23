﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each multicellular creature in the game
/// </summary>
[JsonObject(IsReference = true)]
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/late_multicellular_stage/MulticellularCreature.tscn", UsesEarlyResolve = false)]
[DeserializedCallbackTarget]
public class MulticellularCreature : RigidBody, ISpawned, IProcessable, ISaveLoadedTracked
{
    private static readonly Vector3 SwimUpForce = new(0, 20, 0);

    [JsonProperty]
    private readonly CompoundBag compounds = new(0.0f);

    private Compound atp = null!;
    private Compound glucose = null!;

    [JsonProperty]
    private CreatureAI? ai;

    [JsonProperty]
    private ISpawnSystem? spawnSystem;

#pragma warning disable CA2213
    private MulticellularMetaballDisplayer metaballDisplayer = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private float targetSwimLevel;

    [JsonProperty]
    private float upDownSwimSpeed = 3;

    // TODO: implement
    [JsonIgnore]
    public List<TweakedProcess> ActiveProcesses => new();

    // TODO: implement
    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => compounds;

    // TODO: implement multicellular process statistics
    [JsonIgnore]
    public ProcessStatistics? ProcessStatistics => null;

    [JsonProperty]
    public bool Dead { get; private set; }

    [JsonProperty]
    public Action<MulticellularCreature>? OnDeath { get; set; }

    [JsonProperty]
    public Action<MulticellularCreature, bool>? OnReproductionStatus { get; set; }

    /// <summary>
    ///   The species of this creature. It's mandatory to initialize this with <see cref="ApplySpecies"/> otherwise
    ///   random stuff in this instance won't work
    /// </summary>
    [JsonProperty]
    public LateMulticellularSpecies Species { get; private set; } = null!;

    /// <summary>
    ///   True when this is the player's creature
    /// </summary>
    [JsonProperty]
    public bool IsPlayerCreature { get; private set; }

    /// <summary>
    ///   For checking if the player is in freebuild mode or not
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; private set; } = null!;

    /// <summary>
    ///   Needs access to the world for population changes
    /// </summary>
    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame.GameWorld;

    /// <summary>
    ///   The direction the creature wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection { get; set; } = Vector3.Zero;

    [JsonProperty]
    public MovementMode MovementMode { get; set; }

    [JsonProperty]
    public float TimeUntilNextAIUpdate { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    public int DespawnRadiusSquared { get; set; }

    /// <summary>
    ///   TODO: adjust entity weight once fleshed out
    /// </summary>
    [JsonIgnore]
    public float EntityWeight => 1.0f;

    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        base._Ready();

        atp = SimulationParameters.Instance.GetCompound("atp");
        glucose = SimulationParameters.Instance.GetCompound("glucose");

        metaballDisplayer = GetNode<MulticellularMetaballDisplayer>("MetaballDisplayer");
    }

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(ISpawnSystem spawnSystem, GameProperties currentGame, bool isPlayer)
    {
        this.spawnSystem = spawnSystem;
        CurrentGame = currentGame;
        IsPlayerCreature = isPlayer;

        if (!isPlayer)
            ai = new CreatureAI(this);

        // Needed for immediately applying the species
        _Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // TODO: implement growth
        OnReproductionStatus?.Invoke(this, true);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (MovementMode == MovementMode.Swimming)
        {
            // TODO: apply buoyancy (if this is underwater)

            if (Translation.y < targetSwimLevel)
                ApplyCentralImpulse(Mass * SwimUpForce * delta);

            if (MovementDirection != Vector3.Zero)
            {
                // TODO: movement force calculation
                ApplyCentralImpulse(Mass * MovementDirection * delta);
            }
        }
        else
        {
            if (MovementDirection != Vector3.Zero)
            {
                // TODO: movement force calculation
                ApplyCentralImpulse(Mass * MovementDirection * delta * 50);
            }
        }
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    public void ApplySpecies(Species species)
    {
        if (species is not LateMulticellularSpecies lateSpecies)
            throw new ArgumentException("Only late multicellular species can be used on creatures");

        Species = lateSpecies;

        // TODO: set from species
        compounds.Capacity = 100;

        // TODO: better mass calculation
        Mass = lateSpecies.BodyLayout.Sum(m => m.Size * m.CellType.TotalMass);

        // Setup graphics
        // TODO: handle lateSpecies.Scale
        metaballDisplayer.DisplayFromList(lateSpecies.BodyLayout);
    }

    /// <summary>
    ///   Applies the default movement mode this species has when spawned.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: we probably need to allow spawning in different modes for example amphibian creatures
    ///   </para>
    /// </remarks>
    public void ApplyMovementModeFromSpecies()
    {
        if (Species.ReproductionLocation != ReproductionLocation.Water)
        {
            MovementMode = MovementMode.Walking;
        }
    }

    public void SetInitialCompounds()
    {
        compounds.AddCompound(atp, 50);
        compounds.AddCompound(glucose, 50);
    }

    public MulticellularCreature SpawnOffspring()
    {
        var currentPosition = GlobalTransform.origin;

        // TODO: calculate size somehow
        var separation = new Vector3(10, 0, 0);

        // Create the offspring
        var copyEntity = SpawnHelpers.SpawnCreature(Species, currentPosition + separation,
            GetParent(), SpawnHelpers.LoadMulticellularScene(), true, spawnSystem!, CurrentGame);

        // Make it despawn like normal
        spawnSystem!.AddEntityToTrack(copyEntity);

        // TODO: some kind of resource splitting for the offspring?

        PlaySoundEffect("res://assets/sounds/soundeffects/reproduction.ogg");

        return copyEntity;
    }

    public void BecomeFullyGrown()
    {
        // TODO: implement growth
        // Once growth is added check spawnSystem.IsUnderEntityLimitForReproducing before calling SpawnOffspring
    }

    public void ResetGrowth()
    {
        // TODO: implement growth
    }

    public void Damage(float amount, string source)
    {
        if (IsPlayerCreature && CheatManager.GodMode)
            return;

        if (amount == 0 || Dead)
            return;

        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("damage type is empty");

        // if (amount < 0)
        // {
        //     GD.PrintErr("Trying to deal negative damage");
        //     return;
        // }

        // TODO: sound

        // TODO: show damage visually
        // Flash(1.0f, new Color(1, 0, 0, 0.5f), 1);

        // TODO: hitpoints and death
        // if (Hitpoints <= 0.0f)
        // {
        //     Hitpoints = 0.0f;
        //     Kill();
        // }
    }

    public void PlaySoundEffect(string effect, float volume = 1.0f)
    {
        // TODO: make these sound objects only be loaded once
        // var sound = GD.Load<AudioStream>(effect);

        // TODO: implement sound playing, should probably create a helper method to share with Microbe

        /*// Find a player not in use or create a new one if none are available.
        var player = otherAudioPlayers.Find(nextPlayer => !nextPlayer.Playing);

        if (player == null)
        {
            // If we hit the player limit just return and ignore the sound.
            if (otherAudioPlayers.Count >= Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY)
                return;

            player = new AudioStreamPlayer3D();
            player.MaxDistance = 100.0f;
            player.Bus = "SFX";

            AddChild(player);
            otherAudioPlayers.Add(player);
        }

        player.UnitDb = GD.Linear2Db(volume);
        player.Stream = sound;
        player.Play();*/
    }

    public void SwimUpOrJump(float delta)
    {
        if (MovementMode == MovementMode.Swimming)
        {
            targetSwimLevel += upDownSwimSpeed * delta;
        }
        else
        {
            // TODO: only allow jumping when touching the ground
            ApplyCentralImpulse(new Vector3(0, 1, 0) * delta * 1000);
        }
    }

    public void SwimDownOrCrouch(float delta)
    {
        // TODO: crouching
        targetSwimLevel -= upDownSwimSpeed * delta;
    }
}
