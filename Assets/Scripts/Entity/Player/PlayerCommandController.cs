using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Units;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerCommanderController : MonoBehaviour
    {
        [SerializeField] private LayerMask selectableUnitsLayers;
        [SerializeField] private LayerMask floorLayers;
        [SerializeField] private RectTransform selectionBox;
        private Vector2 startingMousePosition;
        private List<ISelectable> selectedUnits = new(12);
        private HashSet<AbstractUnit> aliveUnits = new(100);
        private HashSet<AbstractUnit> addedUnits = new(24);

        private void OnEnable()
        {
            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent += HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent += HandleUnitSpawn;
        }


        private void OnDisable()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent -= HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent -= HandleUnitSpawn;
        }

        private void HandleUnitSpawn(UnitSpawnEvent evt) => aliveUnits.Add(evt.Unit);

        private void HandleUnitDeselected(UnitDeselectedEvent evt) => selectedUnits.Remove(evt.Unit);

        private void HandleUnitSelected(UnitSelectedEvent evt) => selectedUnits.Add(evt.Unit);

        public void EnterCommanderMode()
        {
           Player.Instance.InCommanderMode = true;
            GridManager.Instance.SetCommanderGridMode(true);
        }

        public void ExitCommanderMode()
        {
            Player.Instance.InCommanderMode = false;
            GridManager.Instance.SetCommanderGridMode(false);
            DeselectAllUnits();       
        }

        public void HandleCommanderUpdate()
        {
            HandleRightClick();
            HandleDragSelect();
        }

        private void HandleDragSelect()
        {
            if (selectionBox == null) { return; }
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseDown();
            }
            else if (Mouse.current.leftButton.isPressed && !Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseDrag();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                HandleMouseUp();
            }
        }

        private void HandleMouseUp()
        {
            if (!Keyboard.current.leftCtrlKey.isPressed)
            {
                DeselectAllUnits();
            }

            HandleLeftClick();

            foreach (AbstractUnit unit in addedUnits)
            {
                unit.Select();
            }

            selectionBox.gameObject.SetActive(false);
            selectionBox.sizeDelta = Vector2.zero;
        }

        private void HandleMouseDrag()
        {
            Bounds selectionBoxBounds = ResizeSelectionBox();

            foreach (AbstractUnit unit in aliveUnits)
            {
                Vector2 unitPosition = Helpers.Camera.WorldToScreenPoint(unit.transform.position);

                if (selectionBoxBounds.Contains(unitPosition))
                {
                    addedUnits.Add(unit);
                }
            }
        }

        private void HandleMouseDown()
        {
            selectionBox.gameObject.SetActive(true);
            startingMousePosition = Mouse.current.position.ReadValue();
            addedUnits.Clear();
        }

        private void DeselectAllUnits()
        {
            ISelectable[] currentlySelectedUnits = selectedUnits.ToArray();
            foreach(ISelectable selectable in currentlySelectedUnits)
            {
                selectable.Deselect();
            }
        }

        private Bounds ResizeSelectionBox()
        {
            Vector2 mousePostion = Mouse.current.position.ReadValue();

            float width = mousePostion.x - startingMousePosition.x;
            float height = mousePostion.y - startingMousePosition.y;

            selectionBox.anchoredPosition = startingMousePosition + new Vector2(width / 2, height / 2);
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

            return new Bounds(selectionBox.anchoredPosition, selectionBox.sizeDelta);
        }

        private void HandleRightClick()
        {
            if (selectedUnits.Count == 0) { return; }

            Ray cameraRay = Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Mouse.current.rightButton.wasReleasedThisFrame && Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, floorLayers))
            {
                List<AbstractUnit> abstractUnits = new List<AbstractUnit>(selectedUnits.Count);

                foreach(ISelectable selectable in selectedUnits)
                {
                    if (selectable is AbstractUnit unit)
                    {
                        abstractUnits.Add(unit);
                    }
                }

                int unitsOnLayer = 0;
                int maxUnitsOnLayer = 1;
                float circleRadius = 0;
                float radialOffset = 0;

                foreach(AbstractUnit unit in abstractUnits)
                {
                    Vector3 targetPosition = new(
                        hit.point.x + circleRadius * Mathf.Cos(radialOffset * unitsOnLayer),
                        hit.point.y,
                        hit.point.z + circleRadius * Mathf.Sin(radialOffset * unitsOnLayer)
                    );

                    unit.MoveTo(targetPosition);
                    unitsOnLayer++;

                    if (unitsOnLayer >= maxUnitsOnLayer)
                    {
                        unitsOnLayer = 0;
                        circleRadius += unit.AgentRadius * 3.5f;
                        maxUnitsOnLayer = Mathf.FloorToInt(2 * Mathf.PI * circleRadius / (unit.AgentRadius * 2));
                        radialOffset = 2 * Mathf.PI / maxUnitsOnLayer;
                    }
                }


                // foreach(ISelectable selectable in selectedUnits)
                // {
                //     if (selectable is IMoveable moveable)
                //     {
                //         moveable.MoveTo(hit.point);
                //     }
                // }
            }
        }

        private void HandleLeftClick()
{
    Ray cameraRay = Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());

    if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, selectableUnitsLayers))
    {
        Debug.Log($"Hit: {hit.collider.name}");

        if (hit.collider.TryGetComponent(out ISelectable selectable))
        {
            Debug.Log($"Selectable found on {hit.collider.name}");
            selectable.Select();
        }
        else
        {
            Debug.LogWarning($"No ISelectable found on {hit.collider.name}");
        }
    }
    else
    {
        Debug.Log("Raycast missed");
    }
}
    }
}