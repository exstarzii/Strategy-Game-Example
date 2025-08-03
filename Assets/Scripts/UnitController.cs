using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public LineRenderer lineRenderer;
    public GameObject attackPreviewCircle;

    [Header("Settings")]
    public float attackPreviewYOffset = 0.05f;

    private Unit selectedUnit;
    private NavMeshPath currentPath;
    private float rightClickTime;
    private const float doubleClickThreshold = 0.3f;

    private Collider[] enemyBuffer = new Collider[32];
    private List<Unit> currentlyTargetable = new List<Unit>();

    void Update()
    {
        HandleLeftClick();
        HandleRightClick();
    }

    void HandleLeftClick()
    {
        if (!Input.GetMouseButtonDown(0) || !TurnManager.Instance.IsMyTurn) return;

        ClearPreview();
        DeselectCurrent();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, Utils.unitLayer))
        {
            Unit unit = hit.collider.GetComponent<Unit>();
            
            if (unit != null && unit.IsOwner)
            {
                selectedUnit = unit;
                selectedUnit.SetSelected(true);
            }
        }
    }

    void HandleRightClick()
    {
        if (selectedUnit == null || !Input.GetMouseButtonUp(1)) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, Utils.unitAndGroundLayer)) return;

        Unit targetUnit = hit.collider.GetComponent<Unit>();

        // Атака
        if (TurnManager.Instance.CanAttack && targetUnit != null && !targetUnit.IsOwner)
        {
            if (selectedUnit.TryAttack(targetUnit))
            {
                ClearPreview();
                return;
            }
        }

        if (targetUnit == null && TurnManager.Instance.CanMove && !Unit.isMoving.Value)
        {
            // Предпросмотр пути
            if (((1 << hit.collider.gameObject.layer) & Utils.groundLayer) != 0)
            {
                PreviewPath(hit.point);
            }

            // Перемещение
            float now = Time.time;
            if (now - rightClickTime < doubleClickThreshold)
            {
                selectedUnit.TryGoByPath(currentPath);
                rightClickTime = 0;
            }
            else
            {
                rightClickTime = now;
            }
        }
    }

    void DeselectCurrent()
    {
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
            selectedUnit = null;
        }

        foreach (var target in currentlyTargetable)
            target?.SetSelected(false);

        currentlyTargetable.Clear();
    }

    void PreviewPath(Vector3 destination)
    {
        if (selectedUnit == null) return;

        ClearPreview();

        if (Utils.CheckPath(selectedUnit, destination, out currentPath))
        {
            lineRenderer.positionCount = currentPath.corners.Length;
            lineRenderer.SetPositions(currentPath.corners);

            ShowAttackPreview(destination);
        }
    }


    void ShowAttackPreview(Vector3 atPosition)
    {
        float range = selectedUnit.attackRange;

        if (attackPreviewCircle != null)
        {
            attackPreviewCircle.SetActive(true);
            Vector3 previewPos = atPosition + Vector3.up * attackPreviewYOffset;
            attackPreviewCircle.transform.position = previewPos;
            attackPreviewCircle.transform.localScale = new Vector3(range*2, range*2, 1);
        }

        currentlyTargetable.Clear();

        int hits = Physics.OverlapSphereNonAlloc(atPosition, range, enemyBuffer, Utils.unitLayer);
        for (int i = 0; i < hits; i++)
        {
            Unit enemy = enemyBuffer[i].GetComponent<Unit>();
            
            if (enemy == null || enemy.IsOwner) continue;

            if (Utils.CanAttack(atPosition, enemy.collider, selectedUnit.attackRange))
            {
                enemy.SetSelected(true);
                currentlyTargetable.Add(enemy);
            }
        }
    }

    void ClearPreview()
    {
        lineRenderer.positionCount = 0;
        if (attackPreviewCircle != null)
            attackPreviewCircle.SetActive(false);

        foreach (var target in currentlyTargetable)
            target?.SetSelected(false);

        currentlyTargetable.Clear();
    }
}
