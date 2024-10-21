using UnityEditor;
using UnityEngine;

namespace LogansThirdPersonCamera
{
	public class ThirdPersonCamera : MonoBehaviour
	{
		[Header("--------------[[ CONFIGS ]]----------------")]
		public LTPCconfig[] MyConfigurations;
		public int CurrentConfigIndex = 0;

		//[Header("---------------[[ REFERENCE (INTERNAL) ]]-----------------")]
		private Transform _trans;
		protected Camera _cam;

		[Header("---------------[[ REFERENCE (EXTERNAL) ]]-----------------")]
		[Tooltip("Transform belonging to the entity (usually the player) that the camera is supposed to follow")]
		public Transform FollowTransform = null;

		//[Header("CACHED VALUES-----------------------")]
		/// <summary>
		/// The distance that the camera should follow away from the target. The camera sets this to the current configuration's follow distance on Start(), but you can use this to change the follow distance after start().
		/// </summary>
		public float CachedFollowDist = 1f;

		/// <summary> Either 1 or -1; cached based on whether NegateHorizontal is true.</summary>
        int hPolarity = 1;
        /// <summary> Either 1 or -1; cached based on whether NegateVertical is true.</summary>
        int vPolarity = 1;

        //[Header("CALCULATED VALUES-----------------------")]
        /// <summary>A vector with only a y and z value (x is 0), that expresses a planar (Y-Z) positioning for the camera at it's max height that can then be rotated around the player to produce an arbitrary X-value.</summary>
        private Vector3 v_RotMax;
        /// <summary>A vector with only a y and z value (x is 0), that expresses a planar (Y-Z) positioning for the camera at it's max height that can then be rotated around the player to produce an arbitrary X-value.</summary>
        private Vector3 v_rotMax_flat_cached;

        /// <summary>A vector with only a y and z value (x is 0), that expresses a planar (Y-Z) positioning for the camera at it's min height that can then be rotated around the player to produce an arbitrary X-value.</summary>
        private Vector3 v_RotMin;
		private Vector3 v_rotMin_flat_cached;

        /// <summary>Because this value gets lerped when changing configs, this holds the calculated (lerped) value.</summary>
        private float calculatedSideOffset;
		/// <summary>Because this value gets lerped when changing configs, this holds the calculated (lerped) value.</summary>
		private float calculatedFollowDistance = 0f;
		/// <summary>This vector gets orbited to describe the correct rotational orbit that the camera should have. Gets added on top of the player's position and the 'origin anchor' vector.</summary>
		public Vector3 vPos_cameraOrbit_calculated = Vector3.back;

		protected Vector3 v_camOriginAnchorPt_calculated = Vector3.zero;

		private float calculatedDistToLookInFrontOfPlayer = 0f;

		//[Header("FLAGS-----------------------")]
		/// <summary>Tells whether camera is currently transitioning to new configuration positioning/orientation.</summary>
		public bool flag_configChangeIsDirty = false;


        [Header("-------------[[ DEBUG ]]---------------")]
		public bool DrawDebugGizmos = false;

		/// <summary>Uses the pythagorean theorem to recalculate the vRotMax vector. Mainly gets called indirectly through the property 'Prop_MaxVertTilt', which itself gets called indirectly when the designer changes it's value in the inspector via the camera's inspector script.</summary>
		public void Update_vRotMax()
		{
			v_RotMax = new Vector3(
				0f, MyConfigurations[CurrentConfigIndex].MaxVertTilt, -Mathf.Sqrt(Mathf.Pow(1, 2f) - Mathf.Pow(MyConfigurations[CurrentConfigIndex].MaxVertTilt, 2f))
			);

			v_rotMax_flat_cached = LTPC_Utils.FlatVect( v_RotMax );
		}

		/// <summary>Uses the pythagorean theorem to recalculate the vRotMin vector. Mainly gets called indirectly through the property 'Prop_MinVertTilt', which itself gets called indirectly when the designer changes it's value in the inspector via the camera's inspector script.</summary>
		public void Update_vRotMin()
		{
			v_RotMin = new Vector3(
				0f, MyConfigurations[CurrentConfigIndex].MinVertTilt, -Mathf.Sqrt(Mathf.Pow(1, 2f) - Mathf.Pow(MyConfigurations[CurrentConfigIndex].MinVertTilt, 2f))
			);

			v_rotMin_flat_cached = LTPC_Utils.FlatVect( v_RotMin );
		}

		void Awake()
		{
			_trans = GetComponent<Transform>();
			_cam = GetComponent<Camera>();
		}

		void Start()
		{
			CheckIfKosher();

			ChangeConfiguration( 0 ); // Default config

			if( FollowTransform != null )
			{
				PlaceCameraAtDefaultPositionAndOrientation();
			}
		}

		//public bool CaseOne, CaseTwo, CaseThree, CaseFour, CaseFive, CaseSix = false;

        public void LateUpdate()
        {
          if ( flag_configChangeIsDirty ) //In the midst of lerping to new config values...
			{
				flag_configChangeIsDirty = false; //reset check...
                /*CaseOne = false;
				CaseTwo = false;
				CaseThree = false;
				CaseFour = false;
				CaseFive = false;
                CaseSix = false;*/

                if ( _cam.fieldOfView != MyConfigurations[CurrentConfigIndex].FOVgoal )
				{
					_cam.fieldOfView = Mathf.Lerp(
						_cam.fieldOfView, MyConfigurations[CurrentConfigIndex].FOVgoal, MyConfigurations[CurrentConfigIndex].Speed_lerpToFOVgoal * Time.deltaTime
					);

					flag_configChangeIsDirty = true;
                    //CaseOne = true;
                }

                if ( calculatedSideOffset != MyConfigurations[CurrentConfigIndex].sideOffsetAmt )
                {
					calculatedSideOffset = LTPC_Utils.Lerp(
						calculatedSideOffset, MyConfigurations[CurrentConfigIndex].sideOffsetAmt, MyConfigurations[CurrentConfigIndex].Speed_lerpToPositioning * Time.deltaTime
					);

                    flag_configChangeIsDirty = true;
                    //CaseTwo = true;

                }

                if ( calculatedFollowDistance != CachedFollowDist )
                {
					calculatedFollowDistance = LTPC_Utils.Lerp(
						calculatedFollowDistance, CachedFollowDist, MyConfigurations[CurrentConfigIndex].Speed_lerpToPositioning * Time.deltaTime
					);

                    //CaseThree = true;

                    flag_configChangeIsDirty = true;
                }

                if ( v_camOriginAnchorPt_calculated != MyConfigurations[CurrentConfigIndex].OriginAnchorPoint )
                {
					v_camOriginAnchorPt_calculated = LTPC_Utils.Lerp(
						v_camOriginAnchorPt_calculated, MyConfigurations[CurrentConfigIndex].OriginAnchorPoint, MyConfigurations[CurrentConfigIndex].Speed_lerpToPositioning * Time.deltaTime
					);

                    //CaseFour = true;

                    flag_configChangeIsDirty = true;
                }

                if ( calculatedDistToLookInFrontOfPlayer != MyConfigurations[CurrentConfigIndex].dist_lookInFrontOfPlayer )
                {
					calculatedDistToLookInFrontOfPlayer = LTPC_Utils.Lerp(
						calculatedDistToLookInFrontOfPlayer, MyConfigurations[CurrentConfigIndex].dist_lookInFrontOfPlayer, MyConfigurations[CurrentConfigIndex].Speed_lerpToPositioning * Time.deltaTime
					);

                    //CaseFive = true;

                    flag_configChangeIsDirty = true;
                }

                if ( MyConfigurations[CurrentConfigIndex].Mode == LTPC_CameraMode.FreeVerticalFixedHorizontal && vPos_cameraOrbit_calculated.x != 0f )
                {
					vPos_cameraOrbit_calculated = LTPC_Utils.Lerp(
						vPos_cameraOrbit_calculated, Vector3.back, MyConfigurations[CurrentConfigIndex].Speed_lerpToPositioning * Time.deltaTime
					);

                    //CaseSix = true;

                    flag_configChangeIsDirty = true;
                }
            }  
        }

        /// <summary>
        /// Typically you would call this in the LateUpdate after the player it is attached to is finished moving.
        /// </summary>
        /// <param name="hAxis"></param>
        /// <param name="vAxis"></param>
        public void UpdateCamera( float hAxis, float vAxis, float timeDelta )
		{
			

            if ( MyConfigurations[CurrentConfigIndex].Mode == LTPC_CameraMode.Fixed )
			{

			}
			else if( MyConfigurations[CurrentConfigIndex].Mode == LTPC_CameraMode.FreeVerticalFixedHorizontal )
			{
                if ( Mathf.Abs(vAxis) > 0f ) //Vertical orbiting -----------------
				{
                    vPos_cameraOrbit_calculated = Quaternion.AngleAxis(vAxis * vPolarity, Vector3.right) * vPos_cameraOrbit_calculated;
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
            }
			else if( MyConfigurations[CurrentConfigIndex].Mode == LTPC_CameraMode.FreeOrbit )
			{
                if ( Mathf.Abs(hAxis) > 0f ) //Horizontal orbiting -----------------
                {
                    vPos_cameraOrbit_calculated = Quaternion.AngleAxis( hAxis * hPolarity, Vector3.up ) * vPos_cameraOrbit_calculated;
                }

                if ( Mathf.Abs(vAxis) > 0f ) //Vertical orbiting -----------------
                {
                    vPos_cameraOrbit_calculated = Quaternion.AngleAxis( vAxis * vPolarity, Vector3.Cross(LTPC_Utils.FlatVect(vPos_cameraOrbit_calculated), Vector3.up) ) * vPos_cameraOrbit_calculated;
                }

                // CORRECT THE TARGET VECTOR VERTICALLY... Note: the reason this isn't encapsulated in the above if-check is because the recoil mechanism also changes this vector
                if ( vPos_cameraOrbit_calculated.y > MyConfigurations[CurrentConfigIndex].MaxVertTilt )
                {
                    vPos_cameraOrbit_calculated = Quaternion.FromToRotation(v_rotMax_flat_cached, LTPC_Utils.FlatVect(vPos_cameraOrbit_calculated)) * v_RotMax;
                }
                else if ( vPos_cameraOrbit_calculated.y < MyConfigurations[CurrentConfigIndex].MinVertTilt )
                {
                    vPos_cameraOrbit_calculated = Quaternion.FromToRotation(v_rotMin_flat_cached, LTPC_Utils.FlatVect(vPos_cameraOrbit_calculated)) * v_RotMin;

                }
            }

			_trans.position = Vector3.Lerp( _trans.position, CalculatedCameraPositionGoal(), MyConfigurations[CurrentConfigIndex].Speed_Move * timeDelta );
			_trans.LookAt( CalculatedLookGoal() );
		}

		/// <summary>Efficiency boolean that tells our script if the last frame had clipping or not./summary>
		bool intersectedOnLastFrame;
		protected Vector3 CalculatedCameraPositionGoal()
		{
			Vector3 vOrigin = FollowTransform.TransformPoint( v_camOriginAnchorPt_calculated );
			Vector3 calculatedGoal = Vector3.zero;

			if ( MyConfigurations[CurrentConfigIndex].Mode == LTPC_CameraMode.FreeVerticalFixedHorizontal )
			{
				calculatedGoal = FollowTransform.TransformPoint(
					v_camOriginAnchorPt_calculated +
					(vPos_cameraOrbit_calculated.normalized * calculatedFollowDistance) +
					(Vector3.right * calculatedSideOffset) +
					(Vector3.Cross(Vector3.right, vPos_cameraOrbit_calculated).normalized * MyConfigurations[CurrentConfigIndex].height_orbit)
				);
			}
			else if( MyConfigurations[CurrentConfigIndex].Mode == LTPC_CameraMode.FreeOrbit )
			{
                calculatedGoal = FollowTransform.TransformPoint(
					v_camOriginAnchorPt_calculated /*+
					(Vector3.right * calculatedSideOffset) +
					(Vector3.Cross(Vector3.right, vPos_cameraOrbit_calculated).normalized * MyConfigurations[CurrentConfigIndex].height_orbit)*/
				) + (vPos_cameraOrbit_calculated.normalized * calculatedFollowDistance);
            }

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
		/// Places camera at default start location and rotation. Only call this when you have the followTransform set.
		/// </summary>
		public void PlaceCameraAtDefaultPositionAndOrientation()
		{
			if( FollowTransform == null )
			{
				Debug.LogError( $"LTPC ERROR! Cannot place camera at default position/orientation if the follow transform reference is null. Returning early..." );
				return;
			}

			calculatedFollowDistance = CachedFollowDist;
			calculatedSideOffset = MyConfigurations[CurrentConfigIndex].sideOffsetAmt;
			vPos_cameraOrbit_calculated = Vector3.back;
			v_camOriginAnchorPt_calculated = MyConfigurations[CurrentConfigIndex].OriginAnchorPoint;
			calculatedDistToLookInFrontOfPlayer = MyConfigurations[CurrentConfigIndex].dist_lookInFrontOfPlayer;

			transform.position = CalculatedCameraPositionGoal(); //using the transform property here instead of the cached variable so that I can call this from the inspector in edit mode..
			transform.LookAt(CalculatedLookGoal());
		}


		public void ChangeConfiguration( int indx, bool lerpToNewConfig = true )
		{
			print($"ChangeConfiguration('{indx}')");
			CurrentConfigIndex = indx;
            hPolarity = MyConfigurations[CurrentConfigIndex].NegateHorizontal ? -1 : 1;
            vPolarity = MyConfigurations[CurrentConfigIndex].NegateVertical ? -1 : 1;

            CachedFollowDist = MyConfigurations[CurrentConfigIndex].Dist_Follow;

			Update_vRotMin();
			Update_vRotMax();

			if( !lerpToNewConfig )
			{
				PlaceCameraAtDefaultPositionAndOrientation();
			}

			flag_configChangeIsDirty = lerpToNewConfig;
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