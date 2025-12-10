using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class HardCodeVideo : MonoBehaviour
{
    public CinemachineSplineDolly camera_di_bo;
    public CinemachineSplineDolly camera_di_bo_2;
    public CinemachineSplineDolly camera_di_doc_bo_song;
    public float previousSpeed;
    public float stopDistance_1 = 233;
    public float stopDistance_2 = 456;
    public float stopDistance_3 = 233;
    public PlayableDirector timeline_bay_len_cao;
    public PlayableDirector timeline_bay_toi_dong_song;

    bool timeline1Played = false;
    bool timeline2Played = false;

    public CinemachineCamera cameraDiBo;
    public CinemachineCamera cameraBayLen;
    public CinemachineCamera cameraBayToiTruyenTHua;
    public CinemachineCamera cameraDiDocBoSong;

    private void Awake()
    {
        timeline_bay_len_cao.stopped += Timeline1_stopped;
        timeline_bay_toi_dong_song.stopped += Timeline2_stopped;
        camera_di_doc_bo_song.AutomaticDolly.Enabled = false;

        cameraDiBo.gameObject.SetActive(true);
        cameraBayLen.gameObject.SetActive(false);
        cameraBayToiTruyenTHua.gameObject.SetActive(false);
        cameraDiDocBoSong.gameObject.SetActive(false);
        camera_di_bo_2.gameObject.SetActive(false);

        camera_di_bo_2.AutomaticDolly.Enabled = false;
    }

    private void Timeline2_stopped(PlayableDirector obj)
    {
        // chạy đoạn từ cái hồ nhỏ đến suối trước cổng xong
        StartCoroutine(StartDollyWithDelay2());
        cameraDiDocBoSong.gameObject.SetActive(true);
        cameraBayToiTruyenTHua.gameObject.SetActive(false);
    }

    private void Timeline1_stopped(PlayableDirector obj)
    {
        // chạy đoạn zoom toàn cảnh xong
        //camera_di_bo.CameraPosition = pausedPosition; // Khôi phục vị trí
        StartCoroutine(StartDollyWithDelay1());
        camera_di_bo_2.gameObject.SetActive(true);

        cameraBayLen.gameObject.SetActive(false);
        camera_di_bo.AutomaticDolly.Enabled = true;
    }

    private IEnumerator StartDollyWithDelay1()
    {
        yield return new WaitForSeconds(2f);
        camera_di_bo_2.AutomaticDolly.Enabled = true;
    }

    private IEnumerator StartDollyWithDelay2()
    {
        yield return new WaitForSeconds(2f);
        camera_di_doc_bo_song.AutomaticDolly.Enabled = true;
    }
    private float pausedPosition;

    private void Update()
    {
        // lần đầu
        if(timeline1Played == false && camera_di_bo.CameraPosition >= stopDistance_1)
        {
            pausedPosition = camera_di_bo.CameraPosition; // Lưu vị trí
            camera_di_bo.AutomaticDolly.Enabled = false;
            camera_di_bo.enabled = false;
            
            cameraDiBo.gameObject.SetActive(false);
            cameraBayLen.gameObject.SetActive(true);
            timeline_bay_len_cao.Play();
            timeline1Played = true;
        }
   
        if (timeline2Played == false && camera_di_bo_2.CameraPosition >= stopDistance_2)
        {
            // bay từ bờ hồ sang
            camera_di_bo_2.AutomaticDolly.Enabled = false;
            camera_di_bo_2.gameObject.SetActive(false);

            cameraBayToiTruyenTHua.gameObject.SetActive(true);
            // đi từ cảnh trên cao xuống
            timeline_bay_toi_dong_song.Play();
            timeline2Played = true;
        }
    }
}
