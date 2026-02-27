using System;
using System.Net.Http;
using UnityEngine;

public class APICourseTest : MonoBehaviour
{
    public string ID;
    public string apiContent;
    public UniWebView uniWebView;
    private async void Awake()
    {
        // using var client = new HttpClient();
        // try
        // {
        //     var response = await client.GetAsync($"https://apis-lms.xheroapp.com/lms/courses/{ID}");
        //     
        //     response.EnsureSuccessStatusCode();
        //     
        //     var json = await response.Content.ReadAsStringAsync();
        //     
        //     Debug.Log("Raw JSON:");
        //     Debug.Log(json);
        // }catch (Exception ex)
        // {
        //     Debug.Log($"Error: {ex.Message}");
        // }
    }

    private void Start()
    {
        
    }
}