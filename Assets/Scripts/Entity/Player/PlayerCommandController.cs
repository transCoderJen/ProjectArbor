using System;
using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.Commands;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Units;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerCommanderController : MonoBehaviour
    {
        [SerializeField] private LayerMask selectableUnitsLayers;
        [SerializeField] private LayerMask floorLayers;
        [SerializeField] private RectTransform selectionBox;


        private BaseCommand activeAction;
        private bool wasMouseDownOnUI;
        private Vector2 startingMousePosition;
        private List<ISelectable> selectedUnits = new(12);
        private HashSet<AbstractUnit> aliveUnits = new(100);
        private HashSet<AbstractUnit> addedUnits = new(24);

        private void OnEnable()
        {
            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent += HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent += HandleUnitSpawn;
            Bus<ActionSelectedEvent>.OnEvent += HandleActionSelected;
        }


        private void OnDisable()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent -= HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent -= HandleUnitSpawn;
            Bus<ActionSelectedEvent>.OnEvent -= HandleActionSelected;
        }

        private void HandleActionSelected(ActionSelectedEvent evt)
        {
            activeAction = evt.Action;
            if (!activeAction.RequiresClickToActivate)
            {
                ActivateAction(new RaycastHit());
            }
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
            if (!wasMouseDownOnUI && activeAction == null && !Keyboard.current.leftCtrlKey.isPressed)
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
            if (activeAction != null || wasMouseDownOnUI) return;

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
            wasMouseDownOnUI = EventSystem.current.IsPointerOverGameObject();
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

            if (Mouse.current.rightButton.wasReleasedThisFrame 
                && Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, floorLayers))
            {
                // find applicable command and issue that command
                List<AbstractUnit> abstractUnits = new List<AbstractUnit>(selectedUnits.Count);

                foreach(ISelectable selectable in selectedUnits)
                {
                    if (selectable is AbstractUnit unit)
                    {
                        abstractUnits.Add(unit);
                    }
                }

                for(int i = 0; i < abstractUnits.Count; i++)
                {
                    CommandContext context = new(abstractUnits[i], hit, i);

                    foreach(ICommand command in abstractUnits[i].AvailableCommands)
                    {
                        if (command.CanHandle(context))
                        {
                            command.Handle(context);
                            break; // To only handle 1 command
                        }
                    }
                }
            }
        }

        private void HandleLeftClick()
        {
            Ray cameraRay = Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (activeAction == null 
                && Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, selectableUnitsLayers)
                && hit.collider.TryGetComponent(out ISelectable selectable))
            {
                selectable.Select();
            }
            else if (activeAction != null
                && !EventSystem.current.IsPointerOverGameObject()
                && Physics.Raycast(cameraRay, out hit, float.MaxValue, floorLayers))
            {
                ActivateAction(hit);
            }
        }

        private void ActivateAction(RaycastHit hit)
        {
            List<AbstractCommandable> abstractCommandables = selectedUnits
                                .Where((unit) => unit is AbstractCommandable)
                                .Cast<AbstractCommandable>()
                                .ToList();

            for (int i = 0; i < abstractCommandables.Count; i++)
            {
                CommandContext context = new(abstractCommandables[i], hit, i);
                activeAction.Handle(context);
            }
            activeAction = null;
        }
    }
}