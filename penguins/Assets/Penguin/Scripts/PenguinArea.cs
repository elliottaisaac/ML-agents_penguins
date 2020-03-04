using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenguinArea : MonoBehaviour
{
    [Tooltip("The agent inside the area")]
    public PenguinAgent penguinAgent;

    [Tooltip("The baby penguin inside the area")]
    public GameObject penguinBaby;

    [Tooltip("The TextMeshPro text that shows the cumulative reward of the agent")]
    public TextMeshPro cumulativeRewardText;

    [Tooltip("Prefab of a live fish")]
    public Fish fishPrefab;

    private List<GameObject> fishList;

    public void ResetArea()
	{
        RemoveALlFish();

        PlacePenguin();

        PlaceBaby();

        SpawnFish(4, Academy.Instance.FloatProperties.GetPropertyWithDefault("fish_speed", 0.5f));
	}

    /// <summary>
    /// Remove a specific fish from the area when it is eaten
    /// </summary>
    /// <param name="fishObject">The fish to remove</param>
    public void RemoveSpecificFish(GameObject fishObject)
    {
        fishList.Remove(fishObject);
        Destroy(fishObject);
    }

    /// <summary>
    /// The number of fish remaining
    /// </summary>
    public int FishRemaining
    {
        get { return fishList.Count; }
    }


    /// <summary>
    /// Choose a random position on the X-Z plane within a partial donut shape
    /// </summary>
    /// <param name="center">The center of the donut</param>
    /// <param name="minAngle">Minimum angle of the wedge</param>
    /// <param name="maxAngle">Maximum angle of the wedge</param>
    /// <param name="minRadius">Minimum distance from the center</param>
    /// <param name="maxRadius">Maximum distance from the center</param>
    /// <returns>A position falling within the specified region</returns>
	///
    public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius)
	{
        float radius = minRadius;
        float angle = minAngle;

        if(maxRadius > minRadius)
		{
            // Pick a random radius
            radius = UnityEngine.Random.Range(minRadius, maxRadius);
        }

        if(maxAngle > minAngle)
		{
            // Pick a random radius
            angle = UnityEngine.Random.Range(minAngle, maxAngle);
        }

        // Center position + forward vector rotated around the Y axis by "angle" degrees, multiplies by "radius"
        return center + Quaternion.Euler(0f, angle, 0f) * vector3.forward * radius;
    }


    /// <summary>
    /// Remove all fish from the area
    /// </summary>
    private void RemoveAllFish()
    {
        if (fishList != null)
        {
            for (int i = 0; i < fishList.Count; i++)
            {
                if (fishList[i] != null)
                {
                    Destroy(fishList[i]);
                }
            }
        }

        fishList = new List<GameObject>();
    }
}
