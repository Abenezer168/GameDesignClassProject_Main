// Isaac Bustad
// 7/27/24

using BugFreeProductions.Extentions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "MoveState_SCO", menuName = "ScriptableObject/MoveStateParam_SCO", order = 0)]
public class MoveStateParam_SCO : ScriptableObject
{
    // Vars
    [SerializeField]
    [Range(0, 200)]
    protected float acceleration = 100;

    [SerializeField]
    [Range(0, 16)]
    protected float maxSpeed = 10;

    [SerializeField, Header("Turning Vars"), Header("As a percentage of duration till full turn")]
    [Range(0, 1)]
    protected float turnSpeed = 0.1f;

    [SerializeField]
    [Range(0, 3)]
    protected float maxTurn = 3;
    
    [SerializeField,Tooltip("Tilt in terms of angle measure the racer tilts while turning ")]
    [Range(0, 90)]
    protected float maxTilt = 45;

    [SerializeField,Range(0,25), Header("Jump Vars")] protected float maxJumpPow = 15;

    [SerializeField,Range(0,25)] protected float jumpMult = 15;
    [SerializeField, Range(1,100)] protected float minJumpPow = 5;

    [SerializeField, Range(0, 1), Header("Fall Gravity Mod"), Header("As a percentage of downward velocity")]
    protected float fallSpeed = 1f;

    // Accessors
    public float Acceleration { get { return acceleration; }  }
    public float MaxSpeed { get { return maxSpeed; } }    
    public float TurnSpeed { get { return turnSpeed; } }
    public float MaxTurn { get { return maxTurn; } }
    public float MaxTilt { get { return maxTilt; } }
    public float MaxJumpPow { get { return maxJumpPow; } }
    public float JumpMult { get {  return jumpMult; } }
    public float MinJumpPow{get{return minJumpPow;}}
    public float FallSpeed { get {  return fallSpeed; } }


}
