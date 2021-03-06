using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MLAgents.Sensor;

namespace MLAgents.Tests
{
    public class RayPerceptionSensorTests
    {
        [Test]
        public void TestGetRayAngles()
        {
            var angles = RayPerceptionSensorComponentBase.GetRayAngles(3, 90f);
            var expectedAngles = new[] { 90f, 60f, 120f, 30f, 150f, 0f, 180f };
            Assert.AreEqual(expectedAngles.Length, angles.Length);
            for (var i = 0; i < angles.Length; i++)
            {
                Assert.AreEqual(expectedAngles[i], angles[i], .01);
            }
        }
    }

    public class RayPerception3DTests
    {
        // Use built-in tags
        const string k_CubeTag = "Player";
        const string k_SphereTag = "Respawn";

        void SetupScene()
        {
            /* Creates game objects in the world for testing.
             *   C is a cube
             *   S are spheres
             *   @ is the agent (at the origin)
             * Each space or line is 5 world units, +x is right, +z is up
             *
             *      C
             *    S   S
             *      @
             *
             *      S
             */
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 0, 10);
            cube.tag = k_CubeTag;

            var sphere1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere1.transform.position = new Vector3(-5, 0, 5);
            sphere1.tag = k_SphereTag;

            var sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere2.transform.position = new Vector3(5, 0, 5);
            // No tag for sphere2

            var sphere3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere3.transform.position = new Vector3(0, 0, -10);
            sphere3.tag = k_SphereTag;

            Physics.SyncTransforms();
        }

        [Test]
        public void TestRaycasts()
        {
            SetupScene();
            var obj = new GameObject("agent");
            var perception = obj.AddComponent<RayPerceptionSensorComponent3D>();

            perception.raysPerDirection = 1;
            perception.maxRayDegrees = 45;
            perception.rayLength = 20;
            perception.detectableTags = new List<string>();
            perception.detectableTags.Add(k_CubeTag);
            perception.detectableTags.Add(k_SphereTag);

            var radii = new[] { 0f, .5f };
            foreach (var castRadius in radii)
            {
                perception.sphereCastRadius = castRadius;
                var sensor = perception.CreateSensor();

                var expectedObs = (2 * perception.raysPerDirection + 1) * (perception.detectableTags.Count + 2);
                Assert.AreEqual(sensor.GetObservationShape()[0], expectedObs);
                var outputBuffer = new float[expectedObs];

                WriteAdapter writer = new WriteAdapter();
                writer.SetTarget(outputBuffer, sensor.GetObservationShape(), 0);

                var numWritten = sensor.Write(writer);
                Assert.AreEqual(numWritten, expectedObs);

                // Expected hits:
                // ray 0 should hit the cube at roughly halfway
                // ray 1 should hit a sphere but no tag
                // ray 2 should hit a sphere with the k_SphereTag tag
                // The hit fraction should be the same for rays 1 and
                //
                Assert.AreEqual(1.0f, outputBuffer[0]); // hit cube
                Assert.AreEqual(0.0f, outputBuffer[1]); // missed sphere
                Assert.AreEqual(0.0f, outputBuffer[2]); // missed unknown tag

                // Hit is at z=9.0 in world space, ray length is 20
                Assert.That(
                    outputBuffer[3], Is.EqualTo((9.5f - castRadius) / perception.rayLength).Within(.0005f)
                );

                // Spheres are at 5,0,5 and 5,0,-5, so 5*sqrt(2) units from origin
                // Minus 1.0 for the sphere radius to get the length of the hit.
                var expectedHitLengthWorldSpace = 5.0f * Mathf.Sqrt(2.0f) - 0.5f - castRadius;
                Assert.AreEqual(0.0f, outputBuffer[4]); // missed cube
                Assert.AreEqual(0.0f, outputBuffer[5]); // missed sphere
                Assert.AreEqual(0.0f, outputBuffer[6]); // hit unknown tag -> all 0
                Assert.That(
                    outputBuffer[7], Is.EqualTo(expectedHitLengthWorldSpace / perception.rayLength).Within(.0005f)
                );

                Assert.AreEqual(0.0f, outputBuffer[8]); // missed cube
                Assert.AreEqual(1.0f, outputBuffer[9]); // hit sphere
                Assert.AreEqual(0.0f, outputBuffer[10]); // missed unknown tag
                Assert.That(
                    outputBuffer[11], Is.EqualTo(expectedHitLengthWorldSpace / perception.rayLength).Within(.0005f)
                );
            }
        }

        [Test]
        public void TestRaycastMiss()
        {
            var obj = new GameObject("agent");
            var perception = obj.AddComponent<RayPerceptionSensorComponent3D>();

            perception.raysPerDirection = 0;
            perception.maxRayDegrees = 45;
            perception.rayLength = 20;
            perception.detectableTags = new List<string>();
            perception.detectableTags.Add(k_CubeTag);
            perception.detectableTags.Add(k_SphereTag);

            var sensor = perception.CreateSensor();
            var expectedObs = (2 * perception.raysPerDirection + 1) * (perception.detectableTags.Count + 2);
            Assert.AreEqual(sensor.GetObservationShape()[0], expectedObs);
            var outputBuffer = new float[expectedObs];

            WriteAdapter writer = new WriteAdapter();
            writer.SetTarget(outputBuffer, sensor.GetObservationShape(), 0);

            var numWritten = sensor.Write(writer);
            Assert.AreEqual(numWritten, expectedObs);

            // Everything missed
            Assert.AreEqual(new float[] { 0, 0, 1, 1 }, outputBuffer);
        }

        [Test]
        public void TestRayFilter()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 0, 10);
            cube.tag = k_CubeTag;
            cube.name = "cubeFar";

            var cubeFiltered = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeFiltered.transform.position = new Vector3(0, 0, 5);
            cubeFiltered.tag = k_CubeTag;
            cubeFiltered.name = "cubeNear";
            cubeFiltered.layer = 7;

            Physics.SyncTransforms();

            var obj = new GameObject("agent");
            var perception = obj.AddComponent<RayPerceptionSensorComponent3D>();
            perception.raysPerDirection = 0;
            perception.rayLength = 20;
            perception.detectableTags = new List<string>();

            var filterCubeLayers = new[] { false, true };
            foreach (var filterCubeLayer in filterCubeLayers)
            {
                // Set the layer mask to either the default, or one that ignores the close cube's layer
                var layerMask = Physics.DefaultRaycastLayers;
                if (filterCubeLayer)
                {
                    layerMask &= ~(1 << cubeFiltered.layer);
                }
                perception.rayLayerMask = layerMask;

                var sensor = perception.CreateSensor();
                var expectedObs = (2 * perception.raysPerDirection + 1) * (perception.detectableTags.Count + 2);
                Assert.AreEqual(sensor.GetObservationShape()[0], expectedObs);
                var outputBuffer = new float[expectedObs];

                WriteAdapter writer = new WriteAdapter();
                writer.SetTarget(outputBuffer, sensor.GetObservationShape(), 0);

                var numWritten = sensor.Write(writer);
                Assert.AreEqual(numWritten, expectedObs);

                if (filterCubeLayer)
                {
                    // Hit the far cube because close was filtered.
                    Assert.That(outputBuffer[outputBuffer.Length - 1],
                        Is.EqualTo((9.5f - perception.sphereCastRadius) / perception.rayLength).Within(.0005f)
                    );
                }
                else
                {
                    // Hit the close cube because not filtered.
                    Assert.That(outputBuffer[outputBuffer.Length - 1],
                        Is.EqualTo((4.5f - perception.sphereCastRadius) / perception.rayLength).Within(.0005f)
                    );
                }
            }
        }

        [Test]
        public void TestRaycastsScaled()
        {
            SetupScene();
            var obj = new GameObject("agent");
            var perception = obj.AddComponent<RayPerceptionSensorComponent3D>();
            obj.transform.localScale = new Vector3(2, 2,2 );

            perception.raysPerDirection = 0;
            perception.maxRayDegrees = 45;
            perception.rayLength = 20;
            perception.detectableTags = new List<string>();
            perception.detectableTags.Add(k_CubeTag);

            var radii = new[] { 0f, .5f };
            foreach (var castRadius in radii)
            {
                perception.sphereCastRadius = castRadius;
                var sensor = perception.CreateSensor();

                var expectedObs = (2 * perception.raysPerDirection + 1) * (perception.detectableTags.Count + 2);
                Assert.AreEqual(sensor.GetObservationShape()[0], expectedObs);
                var outputBuffer = new float[expectedObs];

                WriteAdapter writer = new WriteAdapter();
                writer.SetTarget(outputBuffer, sensor.GetObservationShape(), 0);

                var numWritten = sensor.Write(writer);
                Assert.AreEqual(numWritten, expectedObs);

                // Expected hits:
                // ray 0 should hit the cube at roughly 1/4 way
                //
                Assert.AreEqual(1.0f, outputBuffer[0]); // hit cube
                Assert.AreEqual(0.0f, outputBuffer[1]); // missed unknown tag

                // Hit is at z=9.0 in world space, ray length was 20
                // But scale increases the cast size and the ray length
                var scaledRayLength = 2 * perception.rayLength;
                var scaledCastRadius = 2 * castRadius;
                Assert.That(
                    outputBuffer[2], Is.EqualTo((9.5f - scaledCastRadius) / scaledRayLength).Within(.0005f)
                );
            }
        }

        [Test]
        public void TestRayZeroLength()
        {
            // Place the cube touching the origin
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 0, .5f);
            cube.tag = k_CubeTag;

            Physics.SyncTransforms();

            var obj = new GameObject("agent");
            var perception = obj.AddComponent<RayPerceptionSensorComponent3D>();
            perception.raysPerDirection = 0;
            perception.rayLength = 0.0f;
            perception.sphereCastRadius = .5f;
            perception.detectableTags = new List<string>();
            perception.detectableTags.Add(k_CubeTag);

            {
                // Set the layer mask to either the default, or one that ignores the close cube's layer

                var sensor = perception.CreateSensor();
                var expectedObs = (2 * perception.raysPerDirection + 1) * (perception.detectableTags.Count + 2);
                Assert.AreEqual(sensor.GetObservationShape()[0], expectedObs);
                var outputBuffer = new float[expectedObs];

                WriteAdapter writer = new WriteAdapter();
                writer.SetTarget(outputBuffer, sensor.GetObservationShape(), 0);

                var numWritten = sensor.Write(writer);
                Assert.AreEqual(numWritten, expectedObs);

                // hit fraction is arbitrary but should be finite in [0,1]
                Assert.GreaterOrEqual(outputBuffer[2], 0.0f);
                Assert.LessOrEqual(outputBuffer[2], 1.0f);
            }
        }
    }
}
