using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LogansThirdPersonCamera
{
    [CreateAssetMenu(menuName = "LogansThirdPersonCamera/CameraConfig", fileName = "LTPCcamConfig")]
    public class LTPCconfig : ScriptableObject
    {
        [SerializeField, TextArea(1, 10)] private string optionalDescription;

        public LTPC_CameraMode Mode = LTPC_CameraMode.FreeVerticalFixedHorizontal;

        [Header("-------------[[ SPEEDS ]]---------------")]
		public float Speed_Move = 810f;
        public float Speed_Look = 1;

		[Header("-------------[[ POSITIONAL ]]---------------")]
		[Tooltip("This will be like the \"origin\" or anchor point, relative to the player, where the camera does it's " +
            "movement/rotation from. Should be close to the clavical bone, and should be set from the inspector")]
        [SerializeField] public Vector3 OriginAnchorPoint = new Vector3(0f, 1.4f, 0f);

        [SerializeField] private float dist_follow = 0.646f;
        public float Dist_Follow => dist_follow;

        [Tooltip("Height from the floor (or player's feet) where the vertical orbiting orbits around."), Range(0, 1f)]
        public float height_orbit = 0.488f;
        [Range(0f, 3f), Tooltip("Amount to offset the camera to the side from the origin anchor point.")] 
        public float sideOffsetAmt = 0.311f;

        [Header("--------[[ INTERSECTIONS ]]--------")]
        [Tooltip("Makes the camera resize it's distance based on an environmental intersection")]
        public bool AmHandlingIntersections = true;
        public int IntersectingMask;

		[Header("-------------[[ LOOKING ]]---------------")]
		public float dist_lookInFrontOfPlayer = 2.56f;
        [Range(0f, 1f), Tooltip("Max y height the camera can travel above the player. What actually restricts the camera's vertical movement is 'v_RotMax', which gets updated when you change this value through it's property.")]
        public float MaxVertTilt = 0.81f;

        [Range(0, -1f), Tooltip("Min y height the camera can travel compared to the player. What actually restricts the camera's vertical movement is 'v_RotMin', which gets updated when you change this value through it's property.")]
        public float MinVertTilt = -0.865f;

        /// <summary>Vertical (Y) position at which the camera should draw closer if it goes below.</summary>
        [Tooltip("Vertical (Y) position at which the camera should draw closer if it goes below.")]
        public float pullCloserHeight = 0f;
        [Tooltip("Reverses the direction of the horizontal axis input given to the camera's UpdateCamera() method, if for some reason, you don't want to supply a negated value to the camera through your script.")]
        public bool NegateHorizontal = false;
        [Tooltip("Reverses the direction of the vertical axis input given to the camera's UpdateCamera() method, if for some reason, you don't want to supply a negated value to the camera through your script.")]
        public bool NegateVertical = false;

		[Header("-------------[[ FOV ]]---------------")]
		[Tooltip("Field of View goal for this config")]
        public float FOVgoal = 60f;


		[Header("--------[[ SWITCHING-TO ]]--------")]
		public float Speed_lerpToPositioning = 6.6f;
		[Tooltip("Speed at which to lerp the camera's FOV when going into aim mode.")]
		public float Speed_lerpToFOVgoal = 5.1f;

	}
}