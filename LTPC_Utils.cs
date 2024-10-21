using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LogansThirdPersonCamera
{
    public static class LTPC_Utils 
    {
        /// <summary>
        /// Returns the supplied vector with a y of 0. 
        /// </summary>
        /// <param name="v_passed"></param>
        /// <param name="makeNormalized">Leave true to return a normalized vector, set false to return the vector at its original length.</param>
        /// <returns></returns>
        public static Vector3 FlatVect(Vector3 v_passed, bool makeNormalized = true)
        {
            if (makeNormalized)
                return new Vector3(v_passed.x, 0f, v_passed.z).normalized;
            else
                return new Vector3(v_passed.x, 0f, v_passed.z);

        }

        public static Vector3 SkinnyVect(Vector3 v_passed, bool nrmlzd = true)
        {
            Vector3 v_return = Vector3.zero;

            //if (v_passed.z > 0)
            //    v_return = Quaternion.FromToRotation(flatVect(v_passed), (v_passed.z > 0 ? Vector3.forward : Vector3.back)) * v_passed;

            v_return = new Vector3(0f, v_passed.y, v_passed.z);

            if (nrmlzd)
                v_return = v_return.normalized;

            return v_return;
        }

        public static float Lerp( float a, float b, float t )
        {
            float amt = Mathf.Lerp( a, b, t );

            if( Mathf.Abs(amt - b) < 0.0001f )
            {
                amt = b;
            }

            return amt;
        }

        public static Vector3 Lerp( Vector3 a, Vector3 b, float t )
        {
            Vector3 amt = Vector3.Lerp( a, b, t );

            if ( Vector3.Distance(amt, b) < 0.0001f )
            {
                amt = b;
            }

            return amt;
        }
    }

    /// <summary>
    /// Camera mode for LogansThirdPersonCamera package
    /// </summary>
    public enum LTPC_CameraMode
    {
        /// <summary>
        /// Doesn't allow any orbiting. Always moves to look at a targetted transform at a fixed relative position and rotation.
        /// </summary>
        Fixed = 0,
        /// <summary>
        /// Allows vertical orbiting around a focal transform, but doesn't allow horizontal orbiting, Like a classic third person shooter. IE: Gears of War.
        /// </summary>
        FreeVerticalFixedHorizontal = 1,
        /// <summary>
        /// Allows both vertical and horizontal orbiting around a focal transform. Like a classic hack and slash. IE: God of War.
        /// </summary>
        FreeOrbit = 2,
    }

}
