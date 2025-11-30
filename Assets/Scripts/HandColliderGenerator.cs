using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class HandColliderGenerator : MonoBehaviour
{
    [Tooltip("Prefab con SphereCollider + Rigidbody kinematic (ej. bolita pequeña visible).")]
    public GameObject jointColliderPrefab;

    private XRHandSubsystem handSubsystem;

    private Dictionary<XRHandJointID, GameObject> leftHandColliders = new();
    private Dictionary<XRHandJointID, GameObject> rightHandColliders = new();

   void OnEnable()
    {
        // Buscar el subsistema XR de manos
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];

        // Crear colliders visibles para cada mano (solo joints válidos)
        foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
        {
            // Filtrar Invalid (-1) y EndMarker (valor mayor al último índice válido)
            if (jointId == XRHandJointID.Invalid || jointId == XRHandJointID.EndMarker)
                continue;

            // Mano izquierda
            GameObject leftCol = Instantiate(jointColliderPrefab, transform);
            leftCol.name = "Left_" + jointId + "_Collider";
            leftHandColliders[jointId] = leftCol;

            // Mano derecha
            GameObject rightCol = Instantiate(jointColliderPrefab, transform);
            rightCol.name = "Right_" + jointId + "_Collider";
            rightHandColliders[jointId] = rightCol;
        }
}


    void Update()
    {
        // Si aún no hay subsistema, intentar buscarlo
        if (handSubsystem == null)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
                handSubsystem = subsystems[0];
            return;
        }

        // Mano izquierda
        var leftHand = handSubsystem.leftHand;
        if (leftHand.isTracked)
        {
            foreach (var kv in leftHandColliders)
            {
                XRHandJoint joint = leftHand.GetJoint(kv.Key);
                if (joint.TryGetPose(out Pose pose))
                {
                    kv.Value.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
            }
        }

        // Mano derecha
        var rightHand = handSubsystem.rightHand;
        if (rightHand.isTracked)
        {
            foreach (var kv in rightHandColliders)
            {
                XRHandJoint joint = rightHand.GetJoint(kv.Key);
                if (joint.TryGetPose(out Pose pose))
                {
                    kv.Value.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
            }
        }
    }
}
