using UnityEditor;
using UnityEngine;

namespace LogansThirdPersonCamera
{
	public class ThirdPersonCamera : MonoBehaviour
	{
		[Header("--------------[[ CONFIGS ]]----------------")]
		public LTPCconfig[] MyConfigurations;
		[HideInInspector] public int CurrentConfigIndex = 0;

		//[Header("---------------[[ REFERENCE (INTERNAL) ]]-----------------")]
		private Transform _trans;
		protected Camera _cam;

		[Header("---------------[[ REFERENCE (EXTERNAL) ]]-----------------")]
		[Tooltip("Transform belonging to the entity (usually the player) that the camera is supposed to follow")]
		public Transform FollowTransform = null;


        //[Header("CALCULATED VALUES-----------------------")]
		/// <summary>A vector with only a y and z value (x is 0), that expresses a planar (Y-Z) positioning for the camera at it's max height that can then be rotated around the player to produce an arbitrary X-value.</summary>
		private Vector3 v_RotMax;
        /// <summary>A vector with only a y and z value (x is 0), that expresses a planar (Y-Z) positioning for the camera at it's max height that can then be rotated around the player to produce an arbitrary X-value.</summary>
        public Vector3 V_RotMax => v_RotMax;
		/// <summary>A vector with only a y and z value (x is 0), that expresses a planar (Y-Z) positioning for the camera at it's min height that can then be rotated around the player to produce an arbitrary X-value.</summary>
		private Vector3 v_RotMin;
        /// <summary>A vector with only a y and z value (x is 0), that expresses a planar (Y-Z) positioning for the camera at it's min height that can then be rotated around the player to produce an arbitrary X-value.</summary>
        public Vector3 V_RotMin => v_RotMin;

        /// <summary>Because this value gets lerped when changing configs, this holds the calculated (lerped) value.</summary>
        private float calculatedSideOffset;
		/// <summary>Because this value gets lerped when changing configs, this holds the calculated (lerped) value.</summary>
		private float calculatedFollowDistance = 0f;
		/// <summary>This vector gets orbited to describe the correct rotational orbit that the camera should have. Gets added on top of the player's position and the 'origin anchor' vector.</summary>
		protected Vector3 vPos_cameraOrbit_calculated = Vector3.back;

		protected Vector3 v_camOriginAnchorPt_calculated = Vector3.zero;

		private float calculatedDistToLookInFrontOfPlayer = 0f;


        [Header("-------------[[ DEBUG ]]---------------")]
		public bool DrawDebugGizmos = false;

		/// <summary>Uses the pythagorean theorem to recalculate the vRotMax vector. Mainly gets called indirectly through the property 'Prop_MaxVertTilt', which itself gets called indirectly when the designer changes it's value in the inspector via the camera's inspector script.</summary>
		public void Update_vRotMax()
		{
			v_RotMax = new Vector3(
				0f, MyConfigurations[CurrentConfigIndex].MaxVertTilt, -Mathf.Sqrt(Mathf.Pow(1, 2f) - Mathf.Pow(MyConfigurations[CurrentConfigIndex].MaxVertTilt, 2f))
			);
		}

		/// <summary>Uses the pythagorean theorem to recalculate the vRotMin vector. Mainly gets called indirectly through the property 'Prop_MinVertTilt', which itself gets called indirectly when the designer changes it's value in the inspector via the camera's inspector script.</summary>
		public void Update_vRotMin()
		{
			v_RotMin = new Vector3(
				0f, MyConfigurations[CurrentConfigIndex].MinVertTilt, -Mathf.Sqrt(Mathf.Pow(1, 2f) - Mathf.Pow(MyConfigurations[CurrentConfigIndex].MinVertTilt, 2f))
			);
		}

		void Awake()
		{
			_trans = GetComponent<Transform>();
			_cam = GetComponent<Camera>();
		}

		void Start()
		{
			CheckIfKosher();

			if( FollowTransform != null )
			{
				InitializeCameraValues();
				PlaceCameraAtDefaultPositionAndOrientation();
			}
		}

		/// <summary>
		/// Typically you would call this in the LateUpdate after the player it is attached to is finished moving.
		/// </summary>
		/// <param name="hAxis"></param>
		/// <param name="vAxis"></param>
		public void UpdateCamera( float hAxis, float vAxis, float timeDelta )
		{
			#region Calculate Lerped FOV ------------------------
			_cam.fieldOfView = Mathf.Lerp(
				_cam.fieldOfView, MyConfigurations[CurrentConfigIndex].FOVgoal, MyConfigurations[CurrentConfigIndex].Speed_lerpToFOVgoal * timeDelta
			);

			calculatedSideOffset = Mathf.Lerp(
				calculatedSideOffset, MyConfigurations[CurrentConfigIndex].sideOffsetAmt, MyConfigurations[CurrentConfigIndex].Speed_lerpToNewOffsetPositioning * timeDelta
			);

			calculatedFollowDistance = Mathf.Lerp(
				calculatedFollowDistance, MyConfigurations[CurrentConfigIndex].Dist_follow, MyConfigurations[CurrentConfigIndex].Speed_lerpToNewOffsetPositioning * timeDelta
			);

			v_camOriginAnchorPt_calculated = Vector3.Lerp(
				v_camOriginAnchorPt_calculated, MyConfigurations[CurrentConfigIndex].OriginAnchorPoint, MyConfigurations[CurrentConfigIndex].Speed_lerpToNewOffsetPositioning * timeDelta
			);

			calculatedDistToLookInFrontOfPlayer = Mathf.Lerp(
				calculatedDistToLookInFrontOfPlayer, MyConfigurations[CurrentConfigIndex].dist_lookInFrontOfPlayer, MyConfigurations[CurrentConfigIndex].Speed_lerpToFOVgoal * timeDelta
			);
			#endregion

			// ORBIT THE TARGET VECTOR ------------------///////////////////////
			if ( Mathf.Abs(hAxis) > 0f || Mathf.Abs(vAxis) > 0f )
			{
				//Vertical orbiting -----------------
				vPos_cameraOrbit_calculated = Quaternion.AngleAxis( -vAxis, Vector3.right ) * vPos_cameraOrbit_calculated;
			}

			// CORRECT THE TARGET VECTOR VERTICALLY... Note: the reason this isn't encapsulated in the above if-check is because the recoil mechanism also changes this vector
			if ( vPos_cameraOrbit_calculated.y > MyConfigurations[CurrentConfigIndex].MaxVertTilt )
			{
				vPos_cameraOrbit_calculated = v_RotMax;
			}
			else if ( vPos_cameraOrbit_calculated.y < MyConfigurations[CurrentConfigIndex].MinVertTilt )
			{
				vPos_cameraOrbit_calculated = v_RotMin;
			}

			_trans.position = Vector3.Lerp( _trans.position, CalculatedCameraPositionGoal(), MyConfigurations[CurrentConfigIndex].Speed_Move * timeDelta );
			_trans.LookAt( CalculatedLookGoal() );
		}

		/// <summary>Efficiency boolean that tells our script if the last frame had clipping or not./summary>
		bool intersectedOnLastFrame;
		protected Vector3 CalculatedCameraPositionGoal()
		{
			Vector3 vOrigin = FollowTransform.TransformPoint( v_camOriginAnchorPt_calculated );

			Vector3 calculatedGoal = FollowTransform.TransformPoint(
				v_camOriginAnchorPt_calculated +
				(vPos_cameraOrbit_calculated.normalized * calculatedFollowDistance) +
				(Vector3.right * calculatedSideOffset) +
				(Vector3.Cross(Vector3.right, vPos_cameraOrbit_calculated).normalized * MyConfigurations[CurrentConfigIndex].height_orbit)
			);

			if( MyConfigurations[CurrentConfigIndex].AmHandlingIntersections )
			{
				RaycastHit hitInfo = new RaycastHit();
				float clipValue = -1f;
				if ( Physics.Linecast(vOrigin, calculatedGoal, out hitInfo, MyConfigurations[CurrentConfigIndex].IntersectingMask) )
				{
					calculatedGoal = vOrigin + (Vector3.Normalize(calculatedGoal - vOrigin) * Vector3.Distance(vOrigin, hitInfo.point));

					clipValue = Mathf.Lerp(0.15f, 0.3f, (calculatedGoal - vOrigin).magnitude / 0.86728f);
					_cam.nearClipPlane = clipValue;
					intersectedOnLastFrame = true;
				}
				else //If there's no clipping this frame...
				{
					if (intersectedOnLastFrame) //...and there was clipping last frame - therefore a change in state has occurred...
					{
						_cam.nearClipPlane = 0.3f;
					}
					intersectedOnLastFrame = false;
				}
			}

			return calculatedGoal;

		}

		protected Vector3 CalculatedLookGoal()
		{
			return FollowTransform.TransformPoint(
				v_camOriginAnchorPt_calculated +
				(-vPos_cameraOrbit_calculated.normalized * calculatedDistToLookInFrontOfPlayer)
			);
		}

		/// <summary>
		/// Calculates values such as such as the max/min rotational values based on the settings.
		/// </summary>
		[ContextMenu("call InitializeCamera()")]
		public void InitializeCameraValues()
		{
			Update_vRotMax();
			Update_vRotMin();
		}

		/// <summary>
		/// Places camera at default start location and rotation. Only call this when you have the followTransform set.
		/// </summary>
		public void PlaceCameraAtDefaultPositionAndOrientation()
		{
			if( FollowTransform == null )
			{
				Debug.LogError( $"LTPC ERROR! Cannot place camera at default position/orientation if the follow transform reference is null. Returning early..." );
				return;
			}

			calculatedFollowDistance = MyConfigurations[CurrentConfigIndex].Dist_follow;
			calculatedSideOffset = MyConfigurations[CurrentConfigIndex].sideOffsetAmt;
			vPos_cameraOrbit_calculated = Vector3.back;
			v_camOriginAnchorPt_calculated = MyConfigurations[CurrentConfigIndex].OriginAnchorPoint;
			calculatedDistToLookInFrontOfPlayer = MyConfigurations[CurrentConfigIndex].dist_lookInFrontOfPlayer;

			transform.position = CalculatedCameraPositionGoal(); //using the transform property here instead of the cached variable so that I can call this from the inspector in edit mode..
			transform.LookAt(CalculatedLookGoal());
		}


		public void ChangeConfiguration( int indx )
		{
			CurrentConfigIndex = indx;

		}

		protected float CalculateAimPitch()
		{
			float pitch = 0f;
			int sign = vPos_cameraOrbit_calculated.y >= 0 ? 1 : -1;

			pitch = sign == 1 ? (vPos_cameraOrbit_calculated.y / MyConfigurations[CurrentConfigIndex].MaxVertTilt) * MyConfigurations[CurrentConfigIndex].MaxVertTilt :
				Mathf.Abs(vPos_cameraOrbit_calculated.y / MyConfigurations[CurrentConfigIndex].MinVertTilt * MyConfigurations[CurrentConfigIndex].MinVertTilt);

			return -sign * pitch; //using this now that I've changed the animation float range from -1 to 1
		}

		public void CreateRecoil(Vector3 v_recoil)
		{
			vPos_cameraOrbit_calculated = new Vector3(vPos_cameraOrbit_calculated.x, vPos_cameraOrbit_calculated.y - v_recoil.y, vPos_cameraOrbit_calculated.z);
		}

		public void DebugToggleAction(bool dbg_passed)
		{
			
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if ( !DrawDebugGizmos )
			{
				return;
			}

			Vector3 v = FollowTransform.TransformPoint( MyConfigurations[CurrentConfigIndex].OriginAnchorPoint );
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(v, 0.1f);
			//Handles.DrawLine( v, v + Vector3.up * 0.5f, 0.5f );
			//Handles.Label( v + Vector3.up * 0.5f, nameof(MyStats.v_originAnchor_lcl) );
			Handles.Label(v, "originAnchor");

			v = CalculatedLookGoal();
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(v, 0.2f);
			Handles.DrawDottedLine(v, v + Vector3.up * 1f, 0.5f);
			Handles.DrawLine( FollowTransform.TransformPoint(MyConfigurations[CurrentConfigIndex].OriginAnchorPoint), v );
			Handles.Label(v + Vector3.up * 1f, "Look Goal");

			v = CalculatedCameraPositionGoal();
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(v, 0.2f);
			Handles.DrawLine(v, v + Vector3.up * 0.5f, 0.5f);
			Handles.Label(v + Vector3.up * 0.5f, "Cam Goal");

		}
#endif

		public bool CheckIfKosher()
		{
			bool amKosher = true;

			if ( MyConfigurations == null )
			{
				Debug.LogError($"{nameof(ThirdPersonCamera)}.{nameof(MyConfigurations)} reference was null.");
				amKosher = false;
			}

			return amKosher;
		}
	}
}