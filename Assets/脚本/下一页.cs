using UnityEngine;

public class BookPager : MonoBehaviour
{
    [Header("Pages in order")]
    public GameObject[] pages;

    [Header("Optional")]
    public AudioSource flipSfx;

    private int index = 0;

    void Start()
    {
        // 确保只显示第一页
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(i == index);
    }

    // 绑定到按钮 OnClick
    public void NextPage()
    {
        if (pages == null || pages.Length == 0) return;

        pages[index].SetActive(false);
        index = (index + 1) % pages.Length; // 到最后会回到第一页；不想循环就改一下
        pages[index].SetActive(true);

        if (flipSfx != null) flipSfx.Play();
    }

    public void PrevPage()
    {
        if (pages == null || pages.Length == 0) return;

        pages[index].SetActive(false);
        index = (index - 1 + pages.Length) % pages.Length;
        pages[index].SetActive(true);

        if (flipSfx != null) flipSfx.Play();
    }
}
