using System;
using System.Collections.Generic;
using UnityEngine;

public static class CourseModels
{
    // ================= LIST RESPONSE =================
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
        public CourseLite[] items;
        public CourseLite[] courses;

        public int total;
        public int count;
        public int skip;
        public int limit;
        public int page;
        public int totalPages;
        public bool hasMore;
        public bool hasNextPage;
    }

    [Serializable]
    public class CourseLite
    {
        public string _id;
        public string id;
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

        public List<CourseStartDateItem> courseStartDate;
    }
    [Serializable]
    public class CourseChapter
    {
        public string _id;
        public string title;
        public List<CourseLesson> lessons;
    }

[Serializable]
public class CourseLesson
{
    public string _id;
    public string title;

    public string type;
    public string courseId;
    public string chapterId;

    public int duration;
    public string videoLink;
    public string videoLink2;

    public List<DocAttach> docAttach;
}

[Serializable]
public class DocAttach
{
    public string uri;
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
    }

    [Serializable]
    public class Settings
    {
        public bool needLogin;
    }

    // ================= DETAIL RESPONSE =================
    [Serializable]
    public class CourseDetailResponse
    {
        public bool status;
        public CourseDetail course;
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
        public string introduction;
        public string[] banner;

        public string videoIntro;

        public CoursePriceDetail coursePrice;
        public SeoDetail seo;

        public string sku;
        public string title;
        public string group;
        public string learningMode;

        public string image;
        public int learners;
        public int totalDuration;

        public float stars;
        public int evaluate;

        public Instructor instructor;
        public List<CourseProduct> products;
        public List<CourseChapter> chapters;
        public List<CourseStartDateItem> courseStartDate;

        public List<CourseRelated> upsell;
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

    // ================= MAPPER =================
    public static CourseListItemData ToListItem(CourseLite c)
    {
        if (c == null) return null;

        return new CourseListItemData
        {
            id = !string.IsNullOrWhiteSpace(c._id) ? c._id : c.id,
            title = c.title,
            image = c.image,
            learningMode = c.learningMode,
            stars = c.stars,
            isJoined = c.isJoined,
            group = c.group,
            category = c.category,
            level = c.level,
            learners = c.learners,
            evaluate = c.evaluate,
            totalStudent = c.totalStudent,
            isSelling = c.isSelling,
            promotionText = c.promotionText,

            courseStartDate = c.courseStartDate,

            originalPrice = c.coursePrice != null ? c.coursePrice.originalPrice : 0,
            currentPrice = c.coursePrice != null ? c.coursePrice.currentPrice : 0,
            isFree = c.coursePrice != null && c.coursePrice.isFree,
            isQuotation = c.coursePrice != null && c.coursePrice.isQuotation,
            isContract = c.coursePrice != null && c.coursePrice.isContract,

            seoUrl = c.seo != null ? c.seo.url : null
        };
    }

    [Serializable]
    public class Instructor
    {
        public string _id;
        public string fullName;
        public int learners;
        public int courses;
        public string description;
    }

[Serializable]
public class CourseProduct
{
    public string productName;
    public string image;
    public string externalUrl;

    public string productId;
    public string variantId;

    // Nếu API trả về dạng khác thì giữ thêm các field này để bắt được data
    public string _id;
    public string id;
    public string variant;
    public string defaultVariantId;
}
    [Serializable]
    public class CourseRelated
    {
        public string _id;
        public string title;
        public string image;
        public int learners;
        public float stars;
    }
}
[Serializable]
public class CourseListItemData
{
    public string id;
    public string title;
    public string image;
    public string learningMode;
    public float stars;
    public bool isJoined;
    public string group;
    public string category;
    public string level;
    public int learners;
    public int evaluate;
    public int totalStudent;
    public bool isSelling;
    public string promotionText;

    public long originalPrice;
    public long currentPrice;
    public bool isFree;
    public bool isQuotation;
    public bool isContract;

    public string seoUrl;

    public List<CourseModels.CourseStartDateItem> courseStartDate;
}