using UnityEngine;
using UnityEngine.EventSystems;

namespace MagicLeap.Examples
{
    public class MediaPlayerTimelineHandle : MonoBehaviour // , IEndDragHandler, IBeginDragHandler
    {
        public bool IsDragged { get; private set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragged = true;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragged = false;
        }
    }
}