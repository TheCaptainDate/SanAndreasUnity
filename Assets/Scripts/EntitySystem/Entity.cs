using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base entity class.
/// Should realize base methods to deal with entity.
/// Register / Destroy and such things.
/// 
/// Can also realize IDamagable but this kind of things is better to split into separate modules
/// and add here some kind of "entity modules system".
/// </summary>
public abstract class Entity : MonoBehaviour
{

    /// <summary>
    /// Really dirty way of doing this.
    /// This function is currently needed to update states at spawn of stated entity.
    ///
    /// We can make just make EntOnSpawn() virtual and call base.EntOnSpawn() inside our custom stated entity.
    /// But... I think it's to easy to miss this line of code so for this initional version of ents
    /// system let it be like this.. 
    /// </summary>
    public virtual void _____EntOnSpawnInternal() {}

    /// <summary>
    /// Initialization of entity.
    /// Should be called before spawn.
    /// </summary>
    public abstract void EntInit();
    
    /// <summary>
    /// This is currently "Start" unity's method.
    /// But should be handled by main entites script that rule all stuff.
    /// For now i am ignoring master script since entity is only used for Ped.
    /// 
    /// </summary>
    public abstract void EntOnSpawn();
    
    /// <summary>
    /// Update, tick, think, loop - name is as you want :)
    /// </summary>
    public abstract void EntUpdate();

    /// <summary>
    /// <see cref="_____EntUpdateInternal"/>
    /// Same logic here.
    /// I guess should be moved to vitual but need to discuss.
    /// I saw both variants in other games, so there is no "RIGHT" solution.
    /// TODO
    /// </summary>
    public virtual void _____EntUpdateInternal() {}
    
    /// <summary>
    /// Fixed Update
    /// </summary>
    public abstract void EntFixedUpdate();

    // TODO: all staff below should be replaced with EntitesManager

    private void Update()
    {
        _____EntUpdateInternal();
        EntUpdate();
    }

    private void FixedUpdate()
    {
        EntFixedUpdate();
    }

    private void Start()
    {
        _____EntOnSpawnInternal();
        EntInit();
        EntOnSpawn();
    }
}
