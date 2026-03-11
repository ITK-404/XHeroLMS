using System;
using System.Collections.Generic;
using UnityEngine;

public static class CourseModels
{
    // ================= LIST (Lite) =================
    // API LIST: { status: true, data: { data: [ ... ] } }
    [Serializable]
    public class CourseListResponse
    {
        public bool status;
        public CourseListPayload data;
    }

    [Serializable]
    public class CourseListPayload
    {
        public CourseLite[] data;
    }

    [Serializable]
    public class CourseLite
    {
        public string _id;
        public CoursePriceLite coursePrice;
        public SeoLite seo;
        public Settings settings;

        public string sku;
        public string image;

        public string level;
        public int learners;
        public float stars;
        public int evaluate;

        public string learningMode;
        public object startSellTime;
        public bool isSelling;

        public string title;
        public string group;
        public string category;

        public int totalStudent;
        public bool isJoined;

        public object endTime;
        public string promotionText;

        public List<CourseStartDateItem> courseStartDate; // thêm dòng này
    }
[Serializable]
public class CourseStartDateItem
{
    public CourseDatePart start;
}

[Serializable]
public class CourseDatePart
{
    public int day;
    public int month;
    public int year;
}
    [Serializable]
    public class CoursePriceLite
    {
        public bool isFree;
        public long originalPrice;
        public long currentPrice;

        public bool isQuotation;
        public bool isContract;
    }

    [Serializable]
    public class SeoLite
    {
        public string url;
        // keywords không cần list => bỏ
    }

    [Serializable]
    public class Settings
    {
        public bool needLogin;
    }

    [Serializable]
    public class CourseDetailResponse
    {
        public bool status;
        public CourseDetail data;
    }

    [Serializable]
    public class CourseDetailPayload
    {
        public CourseDetail data;
    }

    [Serializable]
    public class CourseDetail
    {
        public string _id;

        public string description;
        public string[] banner;

        public CoursePriceDetail coursePrice;
        public SeoDetail seo;

        public string sku;
        public string title;
        public string group;
        public string learningMode;
    }

    [Serializable]
    public class CoursePriceDetail
    {
        public bool isFree;
        public long originalPrice;
        public long currentPrice;
        public bool isQuotation;
        public bool isContract;

        public PaymentOption[] paymentOptions;
    }

    [Serializable]
    public class PaymentOption
    {
        public string name;
        public string value;
    }

    [Serializable]
    public class SeoDetail
    {
        public string url;
        public string[] keywords;
    }
}