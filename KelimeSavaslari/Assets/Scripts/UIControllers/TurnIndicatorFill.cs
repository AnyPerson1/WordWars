using System.Collections;
using UnityEngine;
using UnityEngine.UI; // UI bileþenlerini kullanmak için bu satýrý ekleyin

public class ImageFillController : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 20f;
    Coroutine coroutine;

    private void Start()
    {
        fillImage = GetComponent<Image>();
        if (fillImage == null)
        {
            Debug.LogError("Fill Image atanmamýþ! Lütfen Inspector'dan bir Image sürükleyin.");
        }
        if (coroutine != null)
        {
            ResetFill();
        }
        StartReducingFill();
    }


    private void OnEnable()
    {
        StartReducingFill();
    }
    private void OnDisable()
    {
        ResetFill();
    }
    public IEnumerator ReduceFill()
    {
        if (fillImage == null) yield break;
        fillImage.fillAmount = 1f;

        float startFillAmount = fillImage.fillAmount;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            fillImage.fillAmount = Mathf.Lerp(startFillAmount, 0f, timer / duration);
            yield return null;
        }

        fillImage.fillAmount = 0f;
        Debug.Log("Image fillAmount sýfýrlandý!");
    }

    public void StartReducingFill()
    {
        coroutine = StartCoroutine(ReduceFill());
    }

    public void ResetFill()
    {
        StopCoroutine(coroutine);
        fillImage.fillAmount = 1f;
    }
}
