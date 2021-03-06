﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:					Joe Audette
// Created:					2016-09-08
// Last Modified:			2016-09-08
// 


using cloudscribe.SimpleContent.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace cloudscribe.SimpleContent.Storage.EFCore.Models
{
    public class PostEntity : IPost
    {
        public PostEntity()
        {
            categories = new List<string>();
            postComments = new List<PostComment>();

            comments = new List<IComment>();
        }

        public string Id { get; set; }

        public string BlogId { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }

        public string Slug { get; set; }

        public string MetaDescription { get; set; }

        public string Content { get; set; }

        public DateTime PubDate { get; set; } = DateTime.UtcNow;

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; }

        private List<string> categories;
        public List<string> Categories
        {
            get {
                //if(categories.Count == 0)
                var list = CategoriesCsv.Split(new char[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim().ToLower()).ToList();

                categories.AddRange(list.Where(p2 =>
                  categories.All(p1 => p1 != p2)));

                return categories;
            }
            set {
                categories = value;
                CategoriesCsv = string.Join(",", categories);
            }
        }

        public string CategoriesCsv { get; set; } = string.Empty;

        //public List<PostCategory> Cats { get; set; }

        private List<IComment> comments;
        public List<IComment> Comments
        {
            get {
                if(comments.Count == 0)
                {
                    comments.AddRange(PostComments);
                }
                return comments;
            }
            set {
                comments = value;
                if(comments.Count > 0)
                {
                    postComments.Clear();
                    foreach(var c in comments)
                    {
                        postComments.Add(PostComment.FromIComment(c));
                    }
                }
            }
        }

        private List<PostComment> postComments;
        public List<PostComment> PostComments
        {
            get { return postComments; }
            set { postComments = value; }
        }

        public static PostEntity FromIPost(IPost post)
        {
            var p = new PostEntity();
            p.Author = post.Author;
            p.BlogId = post.BlogId;
            p.Categories = post.Categories;
            p.Comments = post.Comments;
            p.Content = post.Content;
            p.Id = post.Id;
            p.IsPublished = post.IsPublished;
            p.LastModified = post.LastModified;
            p.MetaDescription = post.MetaDescription;
            p.PubDate = post.PubDate;
            p.Slug = post.Slug;
            p.Title = post.Title;

            return p;
        }

    }
}
