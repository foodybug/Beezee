using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSeat : MonoBehaviour
{
    [SerializeField] public Transform trnRoot;
    [SerializeField] public float lerpSpeed = 1.0f;
    public bool cardPlacing { get; private set; } = false;
    //public void Set(SabreCard mc, Vector3 p = new Vector3())
    //{
    //    StartCoroutine(Set_CR(mc));
    //}
    IEnumerator Set_CR(SabreCard mc)
    {
        cardPlacing = true;

        mc.transform.SetParent(null);
        float ratio = 0f;

        while (true)
        {
            mc.transform.position = Vector3.Slerp(mc.transform.position, trnRoot.position, ratio);
            mc.transform.rotation = Quaternion.Slerp(mc.transform.rotation, trnRoot.rotation, ratio);
            if((mc.transform.position - trnRoot.position).sqrMagnitude < 0.0001f)
            {
                break;
            }

            yield return null;
            ratio += Time.deltaTime * lerpSpeed;
        }

        mc.transform.SetParent(trnRoot);
        mc.transform.localPosition = Vector3.zero;
        mc.transform.localRotation = Quaternion.identity;

        cardPlacing = false;
    }
    public void Release(Vector3 pos)
    {

    }
}
