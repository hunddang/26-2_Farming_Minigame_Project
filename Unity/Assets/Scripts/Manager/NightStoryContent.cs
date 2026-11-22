using UnityEngine;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed class NightStoryContent : MonoBehaviour
    {
        public GameObject huntObjects, finalObjects, form, cinema, feedEffect;
        public CafeInteractable[] huntTasks, finalTasks;
        public Transform hunter, spirit, memoryActor, trap, identity, securityChair;
        public GameObject leak, powder, closedNet, smoke, bandage, handprint, faceOutline;
        public RawImage cinemaFeed;
        public Transform[] trapMarkers;
        public Text title, body, input, cinemaCaption, feedCaption, wallClock;
        public Text[] choiceLabels;
        public Button[] choices;
        public Image[] cracks;
        public Image darkness;
        public Material darknessMaterial;

        public CafeInteractable Find(StoryAction action, int index = 0)
        {
            var tasks = action == StoryAction.Leak || action == StoryAction.Extinguisher || action == StoryAction.Net ||
                action == StoryAction.Anchor || action == StoryAction.Trap ? huntTasks : finalTasks;
            foreach (var item in tasks)
                if (item.storyAction == action && item.taskIndex == index) return item;
            return null;
        }
    }
}
