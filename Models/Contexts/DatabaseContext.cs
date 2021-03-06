﻿using AnimeListings.Models;
using AnimeListings.Models.Anime;
using AnimeListings.Seeds;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnimeListings.Data
{
    public class DatabaseContext : IdentityDbContext<SeriesUser>
    {

        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            //await new AnimeSeriesSeeds().SeedData();
            base.OnModelCreating(builder);
            // Customize the ASP.NET Core Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Core Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public DbSet<UserAnimeList> UserAnimeLists { get; set; }

        public DbSet<AnimeSeries> AnimeSeries { get; set; }

        public DbSet<AnimeSeriesPictures> AnimeSeriesPicture { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<SeasonsEpisodes> AnimeSeasonsEpisodes { get; set; }

    }
}
