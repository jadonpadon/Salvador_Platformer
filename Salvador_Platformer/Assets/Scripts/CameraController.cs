using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
	public Transform target;
	public Tilemap tilemap;
	public float arrivalTime;
	public Camera followCamera;
	  
	private Vector3 currentVelocity;
	  
	private Vector2 viewportHalfSize;
	private float leftBoundary, rightBoundary, bottomBoundary;

	private Vector2 shakeOffset;
	  
	public void Start()
	{
	tilemap.CompressBounds();
	CalculateBounds();
	}

	private void CalculateBounds()
	{
	viewportHalfSize = new Vector2(followCamera.aspect * followCamera.orthographicSize, followCamera.orthographicSize);
	leftBoundary = tilemap.transform.position.x + tilemap.cellBounds.min.x + viewportHalfSize.x;
	rightBoundary = tilemap.transform.position.x + tilemap.cellBounds.max.x - viewportHalfSize.x;
	bottomBoundary = tilemap.transform.position.y + tilemap.cellBounds.min.y + viewportHalfSize.y;
	}

	public void LateUpdate()
	{
		Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z) + (Vector3)shakeOffset;
		Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, arrivalTime);

		smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, leftBoundary, rightBoundary);
		smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, bottomBoundary, smoothedPosition.y);

		transform.position = smoothedPosition;
	}
	public void Shake(float intensity, float duration)
	{
		StartCoroutine(ShakeCoroutine(intensity, duration));
	}

	private IEnumerator ShakeCoroutine(float intensity, float duration)
	{
		float elapsed = 0f;

		while(elapsed < duration)
		{
			shakeOffset = Random.insideUnitCircle * intensity;
			elapsed += Time.deltaTime;
			yield return null;
		}

		shakeOffset = Vector2.zero;
	}

}