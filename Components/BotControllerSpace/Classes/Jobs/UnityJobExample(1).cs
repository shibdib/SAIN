using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct VertexShiftJob : IJobFor
{
	[ReadOnly] public NativeArray<Vector3> vertices;

	[WriteOnly] public NativeArray<Vector3> newVertices;

	public void Execute(int index)
	{
		Vector3 vertex = vertices[index];

		// Modify each Vector3 by adding 1 to the xyz
		newVertices[index] = new Vector3(vertex.x + 1f, vertex.y + 1f, vertex.z + 1f);
	}
}

public class UnityJobExample : MonoBehaviour
{
	private Mesh mesh;
	private int vertexCount = 10000;
	private bool hasJobFromLastFrame = false;

	private JobHandle vertexJobHandle;
	private VertexShiftJob vertexJob;

	private Vector3[] verticesArray;
	private NativeArray<Vector3> newVerticesNativeArray;

	private void Start()
	{
		mesh = GetComponent<MeshFilter>().mesh;
		verticesArray = mesh.vertices;

		newVerticesNativeArray = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
	}

	private void Update()
	{
		// Ensure the last frame's job is completed
		if (hasJobFromLastFrame)
		{
			vertexJobHandle.Complete();

			// If you want to do something with the data from vertexJob, this is where you would put your code

			// Update the mesh with new vertices
			verticesArray = vertexJob.newVertices.ToArray();
			mesh.SetVertices(verticesArray);
		}

		// Then we start creating the job for the next frame

		// Create a temporary NativeArray to store the data from verticesArray
		var verticesNativeArray = new NativeArray<Vector3>(verticesArray, Allocator.TempJob);

		// Create the job
		vertexJob = new VertexShiftJob
		{
			vertices = verticesNativeArray,
			newVertices = newVerticesNativeArray
		};

		// Schedule the job
		vertexJobHandle = vertexJob.Schedule(vertexCount, new JobHandle());

		// Dispose of temporary NativeArray
		verticesNativeArray.Dispose();

		// Set this bool to true so the job can complete next frame
		hasJobFromLastFrame = true;
	}

	private void OnDestroy()
	{
		// Finish ongoing job
		if (hasJobFromLastFrame)
		{
			vertexJobHandle.Complete();
		}

		// Dispose of persistent NativeArray
		newVerticesNativeArray.Dispose();
	}
}
