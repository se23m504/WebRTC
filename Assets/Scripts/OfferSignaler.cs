using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.WebRTC.Unity
{
    public class OfferSignaler : MonoBehaviour
    {
        public WhepSignaler signaler;

        public void RestartConnection()
        {
            if (signaler == null)
            {
                Debug.LogError("WhepSignaler is not assigned!");
                return;
            }

            signaler.RestartConnection();
        }

        private void ChangeHttpServer(string newHttpServerAddress)
        {
            if (signaler != null)
            {
                signaler.HttpServerAddress = newHttpServerAddress;
                Debug.Log($"HttpServerAddress set to: {signaler.HttpServerAddress}");
                RestartConnection();
            }
            else
            {
                Debug.LogError("WhepSignaler is not assigned!");
            }
        }

        public void UseFirstHttpServer()
        {
            ChangeHttpServer("http://windows.local:8100/mystream/whep");
        }


        public void UseSecondHttpServer()
        {
            ChangeHttpServer("http://windows.local:8200/mystream/whep");
        }
    }
}

