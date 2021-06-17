// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace MagicLeap
{
    /// <summary>
    /// This button is dynamically created by <see cref="UISceneSelector"/>.
    /// It sets the text to given scene name and invokes action when pressed.
    /// </summary>
    public class UISceneSelectionButton : UIToggleButton, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField, Tooltip("UI Text with Scene Name")]
        private Text sceneNameText;

        private string sceneName;
        private Action<string> onSceneButtonClick;
        private bool passingEvent = false;
        private ScrollRect listScrollRect;

        /// <summary>
        /// Initialize the button. Sets correct scene name and sets action to call.
        /// </summary>
        /// <param name="sceneName">Name of the Scene that this button will open</param>
        /// <param name="onSceneButtonClick">Callback for button click</param>
        public void Initialize(string sceneName, ScrollRect listScrollRect, Action<string> onSceneButtonClick)
        {
            this.onSceneButtonClick = onSceneButtonClick;
            this.sceneName = sceneName;
            this.listScrollRect = listScrollRect;
            sceneNameText.text = sceneName;
        }

        /// <summary>
        /// This occurs when button is pressed. 
        /// </summary>
        public override void Pressed()
        {
            base.Pressed();
            onSceneButtonClick?.Invoke(sceneName);
        }

        /// <summary>
        /// Propagate drag event data to scroll rect so its possible to select button and scroll at the same time
        /// </summary>
        /// <param name="pointerEventData"></param>
        public void OnBeginDrag(PointerEventData pointerEventData)
        {
            if (listScrollRect != null)
            {
                ExecuteEvents.Execute(listScrollRect.gameObject, pointerEventData, ExecuteEvents.beginDragHandler);
                passingEvent = true;
            }
        }

        public void OnDrag(PointerEventData pointerEventData)
        {
            if (listScrollRect != null && passingEvent)
            {
                ExecuteEvents.Execute(listScrollRect.gameObject, pointerEventData, ExecuteEvents.dragHandler);
            }
        }

        public void OnEndDrag(PointerEventData pointerEventData)
        {
            if (listScrollRect != null)
            {
                ExecuteEvents.Execute(listScrollRect.gameObject, pointerEventData, ExecuteEvents.endDragHandler);
                passingEvent = false;
            }
        }
    }
}
