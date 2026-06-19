using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;


namespace ShiftedSignal.Garden.UserInterface
{
    public enum MenuName
    {
        Character,
        Skill,
        Inventory,
        Crafting,
        Journal,
         Settings,

    }

    public class UI : Singleton<UI>
    {

        [Header("End Screen")]
        [SerializeField] private UI_FadeScreen fadeScreen;
        [SerializeField] private GameObject endScreen;

        [Header("Radial Menus")]
        [SerializeField] private GameObject ToolSelectorUI;
        [SerializeField] private GameObject WeaponSelectorUI;

        [Header("Menus")]
        [SerializeField] private GameObject characterUI;
        [SerializeField] private GameObject skillTreeUI;
        [SerializeField] private GameObject InventoryUI;
        [SerializeField] private GameObject craftingUI;
        [SerializeField] private GameObject settingsUI;
        [SerializeField] private GameObject inGameUI;
        [SerializeField] private GameObject[] menuItems;
        private MenuName selectedMenu;

        [Header("Tooltip Compnents")]
        public UI_ItemTooltip ItemToolTip;

        [Header("Inputs")]
        public InputActionReference toolSelectorInput;
        public InputActionReference menuInput;
        public InputActionReference toggleMenuRight;
        public InputActionReference toggleMenuLeft;
        
        private UnscaledInvoke unscaledInvoke;
        
        void Start()
        {
            SwitchTo(inGameUI);
        }

        public void SwitchTo(GameObject _menu)
        {
            DeactivateAllMenus();

            if (_menu != null)
                _menu.SetActive(true);
        }

        public bool IsMenuOpen()
        {
            for (int i = 0; i < menuItems.Length;i ++)
            {
                if (menuItems[i].gameObject.activeSelf)
                    return true;
            }
            
            return false;
        }

        public void SwitchToInGameUI()
        {
            DeactivateAllMenus();

            inGameUI.SetActive(true);
        }

        public void DeactivateAllMenus()
        {
            foreach (GameObject menu in menuItems)
            {
                menu.gameObject.SetActive(false);
            }
            inGameUI.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (menuInput.action.WasPressedThisFrame())
            {
                if (IsMenuOpen())
                {
                    Time.timeScale = 1;
                    SwitchTo(inGameUI);
                    ItemToolTip.gameObject.SetActive(false);

                }
                else
                {
                    Time.timeScale = 0;
                    SwitchTo(characterUI);
                    selectedMenu = MenuName.Character;
                }
            }

            if (toolSelectorInput.action.IsPressed() && !IsMenuOpen() && !Player.Instance.InManagementState && !Player.Instance.InCommanderMode)
            {
                ToolSelectorUI.SetActive(true);
            }
            else
            {
                ToolSelectorUI.SetActive(false);
            }

            if (IsMenuOpen())
            {
                if (toggleMenuLeft.action.WasPressedThisFrame())
                {
                    int menuCount = Enum.GetNames(typeof(MenuName)).Length;
                    selectedMenu = (MenuName)(((int)selectedMenu - 1 + menuCount) % menuCount);
                    SwitchTo(menuItems[(int)selectedMenu]);
                }
                else if (toggleMenuRight.action.WasPressedThisFrame())
                {
                    selectedMenu = (MenuName)(((int)selectedMenu + 1) % Enum.GetNames(typeof(MenuName)).Length);
                    SwitchTo(menuItems[(int)selectedMenu]);
                }
            }
        }

        
    }
}
