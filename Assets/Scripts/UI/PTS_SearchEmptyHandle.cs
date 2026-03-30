using UnityEngine;

public class PTS_SearchEmptyHandle : MonoBehaviour
{
    [SerializeField] private Frame_EmptySearchCouse Frame_EmptySearchCouse;
    
    [SerializeField] private CourseSearch courseSearch;
    private void Awake()
    {
        courseSearch.ShowEmptySearch += CourseSearchOnOnResultsChanged;
    }

    private void OnDestroy()
    {
        courseSearch.ShowEmptySearch -= CourseSearchOnOnResultsChanged;
    }
    
    private void CourseSearchOnOnResultsChanged(bool isShow)
    {
        if (isShow)
        {
            Frame_EmptySearchCouse.Show();
        }
        else
        {
            Frame_EmptySearchCouse.Hide();
        }
    }
}