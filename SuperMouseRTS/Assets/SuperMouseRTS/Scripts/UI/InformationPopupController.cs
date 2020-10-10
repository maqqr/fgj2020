using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.SuperMouseRTS.Scripts.UI
{
    public class InformationPopupController : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI text;
        private static InformationPopupController instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        internal void ShowPopup(string infoText)
        {
            gameObject.SetActive(true);
            text.text = infoText;
            
        }

        public static void ShowFactoryInformation(float3 position, OreResources resources, SpawnScheduler spawn)
        {
            instance.PositionPopup(new Vector3(position.x, position.y, position.z));
            var spawns = spawn.SpawnsOrdered + spawn.SpawnsUnderAway;
            instance.ShowPopup(
                $"Ore: {resources.Value}{Environment.NewLine}" +
                (spawns > 0 ? $"Spawning: {spawns}" : ""));
        }

        private void PositionPopup(Vector3 vector3)
        {
            transform.position = vector3;
        }

        public static void DisablePopup()
        {
            instance.DisablePopupWindow();
        }

        internal void DisablePopupWindow()
        {
            gameObject.SetActive(false);
        }
    }
}
