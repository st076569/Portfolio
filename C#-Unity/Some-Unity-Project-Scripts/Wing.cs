using System;
using UnityEngine;

namespace TestPhysics
{
    [Serializable]
    public class WingData
    {
        public float Square = 1.0f; // [m^2]
        public float CxMin = 0.02f; // in [0.02, 0.06]
        public float CyMin = 0.40f; // in [0, 0.4]
        public float CxMax = 1.20f; // in [1.1, 1.3]
        public float CyMax = 1.20f; // in [1, 1.7]
        public float Cz = 0.01f;    // in [0.01, 0.03]
    }

    [Serializable]
    public class FlapData
    {
        public float RelativeLength = 0.3f; // in [0, 1]
        public float RelativeAngle = 0.0f;  // in [-1, 1]
        public float MaxAngle = 45f;        // [Deg], in [-90, 90]
    }

    public class Wing : AerodynamicObject
    {
        [Space]
        public WingData WingParameters;
        public FlapData FlapParameters;

        [HideInInspector]
        public float Cx = 0.0f;
        [HideInInspector]
        public float Cy = 0.0f;

        public void ComputeCoefficients(float sa, float ca, float st, float ct)
        {
            // Relative drag force coefficient
            Cx = ca * (WingParameters.CxMin - WingParameters.CyMin * ca * sa + 4.4f * (WingParameters.CyMin - WingParameters.CyMax) * Mathf.Pow(ca, 12f) * 
                Mathf.Pow(sa, 2f) + WingParameters.CxMax * FlapParameters.RelativeLength * Mathf.Pow(ca * st, 2f) + Mathf.Pow(sa, 2f) * (WingParameters.CxMax - 
                WingParameters.CxMin + WingParameters.CyMin - WingParameters.CyMax + WingParameters.CxMax * FlapParameters.RelativeLength * Mathf.Pow(st, 2f)));

            // Relative lifting force coefficient
            Cy = WingParameters.CxMin * sa - (WingParameters.CyMin - WingParameters.CyMax) * Mathf.Pow(ca, 2f) * sa + 4.4f * (WingParameters.CyMax - 
                WingParameters.CyMin) * Mathf.Pow(ca, 14f) * sa + (WingParameters.CxMax - WingParameters.CxMin) * Mathf.Pow(sa, 3f) + FlapParameters.RelativeLength * 
                ca * Mathf.Pow(sa, 2f) * 2f * st * ct + Mathf.Pow(ca, 3f) * (WingParameters.CyMin + FlapParameters.RelativeLength * 2f * st * ct);
        }

        public void UpdateAerodynamicForce()
        {
            // Compute relative velocity of the flow
            Vector3 airRelVelocity3D = transform.InverseTransformVector(AirParameters.Velocity - ConnectedBody.GetPointVelocity(transform.position));
            Vector3 aerosolRelVelocity3D = transform.InverseTransformVector(AerosolParameters.Velocity - ConnectedBody.GetPointVelocity(transform.position));
            Vector2 airRelVelocity2D = airRelVelocity3D;

            // Compute aerodynamic coefficients
            ComputeCoefficients(airRelVelocity2D.normalized.y, airRelVelocity2D.normalized.x, Mathf.Sin(Mathf.Deg2Rad * FlapParameters.RelativeAngle * 
                FlapParameters.MaxAngle), Mathf.Cos(Mathf.Deg2Rad * FlapParameters.RelativeAngle * FlapParameters.MaxAngle));

            // Compute flow dynamic pressure
            var airDynamicPressure = 0.5f * AirParameters.Density * Mathf.Pow(airRelVelocity2D.magnitude, 2f);
            var airWingDynamicPressure = 0.5f * AirParameters.Density * Mathf.Abs(airRelVelocity3D.z) * airRelVelocity3D.z;
            Vector3 aerosolDynamicPressure = 0.5f * AerosolParameters.Density * aerosolRelVelocity3D.magnitude * aerosolRelVelocity3D;

            // Compute aerodynamic forces in relative to the wing coordinate system
            Force.x = WingParameters.Square * (Cx * airDynamicPressure + WingParameters.CxMin * aerosolDynamicPressure.x);
            Force.y = WingParameters.Square * (Cy * airDynamicPressure + WingParameters.CxMax * aerosolDynamicPressure.y);
            Force.z = WingParameters.Square * WingParameters.Cz * (airWingDynamicPressure + aerosolDynamicPressure.z);
            Torque = Vector3.zero;
        }

        protected override void FixedUpdate()
        {
            UpdateAerodynamicForce();
            base.FixedUpdate();
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            Cx = 0;
            Cy = 0;
        }
    }
}
