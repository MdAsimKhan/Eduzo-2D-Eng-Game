using UnityEngine;

public class LaneTouchManager : MonoBehaviour
{
    public Camera cam;
    public GrammarBowlingGame gameManager;

    void Update()
    {
        // MOUSE (Editor / PC)
        if (Input.GetMouseButtonDown(0))
        {
            HandleTap(Input.mousePosition);
        }

        // TOUCH (Mobile)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTap(Input.GetTouch(0).position);
        }
    }

    void HandleTap(Vector2 screenPos)
    {
        Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            LaneSelector lane = hit.collider.GetComponent<LaneSelector>();
            if (lane != null)
            {
                gameManager.SelectLane(lane.laneIndex);
            }
        }
    }
}