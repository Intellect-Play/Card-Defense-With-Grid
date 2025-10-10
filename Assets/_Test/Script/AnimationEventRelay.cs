using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    //private OtherScript otherScript;

    private void Awake()
    {
        // Obyektin üstündəki başqa scripti tapırıq
        //otherScript = GetComponent<OtherScript>();
    }

    // Animator-dan gələn Event bunu çağıracaq
    public void OnAttackFrame()
    {
        RandomWeaponSpawner.instance.ChangePosWeapons();
        //Debug.Log("Animation event gəldi, indi başqa scriptə ötürürəm");
        //if (otherScript != null)
        //{
        //    otherScript.DoDamage(); // başqa scriptdəki funksiyanı çağır
        //}
    }
}
