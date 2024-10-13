﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shizou.Data.Database;

#nullable disable

namespace Shizou.Data.Migrations
{
    [DbContext(typeof(ShizouContext))]
    [Migration("20241013092037_AddCreators")]
    partial class AddCreators
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("Shizou.Data.Models.AniDbAnime", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AirDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("AniDbUpdated")
                        .HasColumnType("TEXT");

                    b.Property<int>("AnimeType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("EndDate")
                        .HasColumnType("TEXT");

                    b.Property<int?>("EpisodeCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImageFilename")
                        .HasColumnType("TEXT");

                    b.Property<float?>("Rating")
                        .HasColumnType("REAL");

                    b.Property<bool>("Restricted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TitleEngish")
                        .HasColumnType("TEXT");

                    b.Property<string>("TitleOriginal")
                        .HasColumnType("TEXT");

                    b.Property<string>("TitleTranscription")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AniDbAnimes");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbAnimeRelation", b =>
                {
                    b.Property<int>("AnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ToAnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RelationType")
                        .HasColumnType("INTEGER");

                    b.HasKey("AnimeId", "ToAnimeId", "RelationType");

                    b.ToTable("AniDbAnimeRelations");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbCharacter", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImageFilename")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("AniDbCharacters");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbCreator", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImageFilename")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("AniDbCreators");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbCredit", b =>
                {
                    b.Property<int>("AniDbAnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AniDbCharacterId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AniDbCreatorId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Role")
                        .HasColumnType("TEXT");

                    b.HasKey("AniDbAnimeId", "Id");

                    b.HasIndex("AniDbCharacterId");

                    b.HasIndex("AniDbCreatorId");

                    b.ToTable("AniDbCredits");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbEpisode", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("AirDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("AniDbAnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DurationMinutes")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EpisodeType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Summary")
                        .HasColumnType("TEXT");

                    b.Property<string>("TitleEnglish")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TitleOriginal")
                        .HasColumnType("TEXT");

                    b.Property<string>("TitleTranscription")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AniDbAnimeId");

                    b.ToTable("AniDbEpisodes");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbEpisodeFileXref", b =>
                {
                    b.Property<int>("AniDbEpisodeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AniDbFileId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AniDbEpisodeId", "AniDbFileId");

                    b.HasIndex("AniDbFileId");

                    b.ToTable("AniDbEpisodeFileXrefs");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbFile", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(21)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AniDbFiles");

                    b.HasDiscriminator().HasValue("AniDbFile");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbGroup", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ShortName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AniDbGroups");
                });

            modelBuilder.Entity("Shizou.Data.Models.AnimeFilter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Criteria")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AnimeFilters");
                });

            modelBuilder.Entity("Shizou.Data.Models.CommandRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CommandArgs")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CommandId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<int>("QueueType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CommandId")
                        .IsUnique();

                    b.ToTable("CommandRequests");
                });

            modelBuilder.Entity("Shizou.Data.Models.FileWatchedState", b =>
                {
                    b.Property<int>("AniDbFileId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("MyListId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Watched")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("WatchedUpdated")
                        .HasColumnType("TEXT");

                    b.HasKey("AniDbFileId");

                    b.HasIndex("MyListId");

                    b.ToTable("FileWatchedStates");
                });

            modelBuilder.Entity("Shizou.Data.Models.HangingEpisodeFileXref", b =>
                {
                    b.Property<int>("AniDbEpisodeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AniDbNormalFileId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AniDbEpisodeId", "AniDbNormalFileId");

                    b.HasIndex("AniDbNormalFileId");

                    b.ToTable("HangingEpisodeFileXrefs");
                });

            modelBuilder.Entity("Shizou.Data.Models.ImportFolder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("ScanOnImport")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("Path")
                        .IsUnique();

                    b.ToTable("ImportFolders");
                });

            modelBuilder.Entity("Shizou.Data.Models.LocalFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AniDbFileId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Crc")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Ed2k")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("FileSize")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Ignored")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ImportFolderId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PathTail")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Signature")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Updated")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AniDbFileId");

                    b.HasIndex("Ed2k")
                        .IsUnique();

                    b.HasIndex("Signature")
                        .IsUnique();

                    b.HasIndex("ImportFolderId", "PathTail")
                        .IsUnique();

                    b.ToTable("LocalFiles");
                });

            modelBuilder.Entity("Shizou.Data.Models.MalAniDbXref", b =>
                {
                    b.Property<int>("MalAnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AniDbAnimeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("MalAnimeId", "AniDbAnimeId");

                    b.HasIndex("AniDbAnimeId");

                    b.ToTable("MalAniDbXrefs");
                });

            modelBuilder.Entity("Shizou.Data.Models.MalAnime", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AnimeType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("EpisodeCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("MalAnimes");
                });

            modelBuilder.Entity("Shizou.Data.Models.ScheduledCommand", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CommandArgs")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CommandId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double?>("FrequencyMinutes")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("NextRunTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<int>("QueueType")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("RunsLeft")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CommandId")
                        .IsUnique();

                    b.ToTable("ScheduledCommands");
                });

            modelBuilder.Entity("Shizou.Data.Models.Timer", b =>
                {
                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExtraId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("TEXT");

                    b.HasKey("Type", "ExtraId");

                    b.ToTable("Timers");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbGenericFile", b =>
                {
                    b.HasBaseType("Shizou.Data.Models.AniDbFile");

                    b.HasDiscriminator().HasValue("AniDbGenericFile");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbNormalFile", b =>
                {
                    b.HasBaseType("Shizou.Data.Models.AniDbFile");

                    b.Property<int?>("AniDbGroupId")
                        .HasColumnType("INTEGER");

                    b.Property<bool?>("Censored")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Chaptered")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Crc")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Deprecated")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DurationSeconds")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Ed2k")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("FileSize")
                        .HasColumnType("INTEGER");

                    b.Property<int>("FileVersion")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Md5")
                        .HasColumnType("TEXT");

                    b.Property<string>("Sha1")
                        .HasColumnType("TEXT");

                    b.Property<string>("Source")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("TEXT");

                    b.HasIndex("AniDbGroupId");

                    b.HasIndex("Ed2k")
                        .IsUnique();

                    b.HasDiscriminator().HasValue("AniDbNormalFile");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbCredit", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbAnime", "AniDbAnime")
                        .WithMany("AniDbCredits")
                        .HasForeignKey("AniDbAnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Shizou.Data.Models.AniDbCharacter", "AniDbCharacter")
                        .WithMany("AniDbCredits")
                        .HasForeignKey("AniDbCharacterId");

                    b.HasOne("Shizou.Data.Models.AniDbCreator", "AniDbCreator")
                        .WithMany("AniDbCredits")
                        .HasForeignKey("AniDbCreatorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AniDbAnime");

                    b.Navigation("AniDbCharacter");

                    b.Navigation("AniDbCreator");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbEpisode", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbAnime", "AniDbAnime")
                        .WithMany("AniDbEpisodes")
                        .HasForeignKey("AniDbAnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AniDbAnime");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbEpisodeFileXref", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbEpisode", "AniDbEpisode")
                        .WithMany("AniDbEpisodeFileXrefs")
                        .HasForeignKey("AniDbEpisodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Shizou.Data.Models.AniDbFile", "AniDbFile")
                        .WithMany("AniDbEpisodeFileXrefs")
                        .HasForeignKey("AniDbFileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AniDbEpisode");

                    b.Navigation("AniDbFile");
                });

            modelBuilder.Entity("Shizou.Data.Models.FileWatchedState", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbFile", "AniDbFile")
                        .WithOne("FileWatchedState")
                        .HasForeignKey("Shizou.Data.Models.FileWatchedState", "AniDbFileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AniDbFile");
                });

            modelBuilder.Entity("Shizou.Data.Models.HangingEpisodeFileXref", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbNormalFile", "AniDbNormalFile")
                        .WithMany("HangingEpisodeFileXrefs")
                        .HasForeignKey("AniDbNormalFileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AniDbNormalFile");
                });

            modelBuilder.Entity("Shizou.Data.Models.LocalFile", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbFile", "AniDbFile")
                        .WithMany("LocalFiles")
                        .HasForeignKey("AniDbFileId");

                    b.HasOne("Shizou.Data.Models.ImportFolder", "ImportFolder")
                        .WithMany("LocalFiles")
                        .HasForeignKey("ImportFolderId");

                    b.Navigation("AniDbFile");

                    b.Navigation("ImportFolder");
                });

            modelBuilder.Entity("Shizou.Data.Models.MalAniDbXref", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbAnime", "AniDbAnime")
                        .WithMany("MalAniDbXrefs")
                        .HasForeignKey("AniDbAnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Shizou.Data.Models.MalAnime", "MalAnime")
                        .WithMany("MalAniDbXrefs")
                        .HasForeignKey("MalAnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AniDbAnime");

                    b.Navigation("MalAnime");
                });

            modelBuilder.Entity("Shizou.Data.Models.MalAnime", b =>
                {
                    b.OwnsOne("Shizou.Data.Models.MalStatus", "Status", b1 =>
                        {
                            b1.Property<int>("MalAnimeId")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("State")
                                .HasColumnType("INTEGER");

                            b1.Property<DateTime>("Updated")
                                .HasColumnType("TEXT");

                            b1.Property<int>("WatchedEpisodes")
                                .HasColumnType("INTEGER");

                            b1.HasKey("MalAnimeId");

                            b1.ToTable("MalAnimes");

                            b1.WithOwner()
                                .HasForeignKey("MalAnimeId");
                        });

                    b.Navigation("Status");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbNormalFile", b =>
                {
                    b.HasOne("Shizou.Data.Models.AniDbGroup", "AniDbGroup")
                        .WithMany("AniDbFiles")
                        .HasForeignKey("AniDbGroupId");

                    b.OwnsMany("Shizou.Data.Models.AniDbAudio", "Audio", b1 =>
                        {
                            b1.Property<int>("AniDbNormalFileId")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Bitrate")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("Codec")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Language")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.HasKey("AniDbNormalFileId", "Id");

                            b1.ToTable("AniDbFiles");

                            b1.ToJson("Audio");

                            b1.WithOwner()
                                .HasForeignKey("AniDbNormalFileId");
                        });

                    b.OwnsMany("Shizou.Data.Models.AniDbSubtitle", "Subtitles", b1 =>
                        {
                            b1.Property<int>("AniDbNormalFileId")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("INTEGER");

                            b1.Property<string>("Language")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.HasKey("AniDbNormalFileId", "Id");

                            b1.ToTable("AniDbFiles");

                            b1.ToJson("Subtitles");

                            b1.WithOwner()
                                .HasForeignKey("AniDbNormalFileId");
                        });

                    b.OwnsOne("Shizou.Data.Models.AniDbVideo", "Video", b1 =>
                        {
                            b1.Property<int>("AniDbNormalFileId")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("BitRate")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("Codec")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<int>("ColorDepth")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Height")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Width")
                                .HasColumnType("INTEGER");

                            b1.HasKey("AniDbNormalFileId");

                            b1.ToTable("AniDbFiles");

                            b1.ToJson("Video");

                            b1.WithOwner()
                                .HasForeignKey("AniDbNormalFileId");
                        });

                    b.Navigation("AniDbGroup");

                    b.Navigation("Audio");

                    b.Navigation("Subtitles");

                    b.Navigation("Video");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbAnime", b =>
                {
                    b.Navigation("AniDbCredits");

                    b.Navigation("AniDbEpisodes");

                    b.Navigation("MalAniDbXrefs");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbCharacter", b =>
                {
                    b.Navigation("AniDbCredits");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbCreator", b =>
                {
                    b.Navigation("AniDbCredits");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbEpisode", b =>
                {
                    b.Navigation("AniDbEpisodeFileXrefs");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbFile", b =>
                {
                    b.Navigation("AniDbEpisodeFileXrefs");

                    b.Navigation("FileWatchedState")
                        .IsRequired();

                    b.Navigation("LocalFiles");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbGroup", b =>
                {
                    b.Navigation("AniDbFiles");
                });

            modelBuilder.Entity("Shizou.Data.Models.ImportFolder", b =>
                {
                    b.Navigation("LocalFiles");
                });

            modelBuilder.Entity("Shizou.Data.Models.MalAnime", b =>
                {
                    b.Navigation("MalAniDbXrefs");
                });

            modelBuilder.Entity("Shizou.Data.Models.AniDbNormalFile", b =>
                {
                    b.Navigation("HangingEpisodeFileXrefs");
                });
#pragma warning restore 612, 618
        }
    }
}
