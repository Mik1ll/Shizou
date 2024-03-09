using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class FileHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HangingEpisodeFileXrefs_AniDbFiles_AniDbFileId",
                table: "HangingEpisodeFileXrefs");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalFiles_AniDbEpisodes_ManualLinkEpisodeId",
                table: "LocalFiles");

            migrationBuilder.DropIndex(
                name: "IX_LocalFiles_AniDbFileId",
                table: "LocalFiles");

            migrationBuilder.DropIndex(
                name: "IX_LocalFiles_ManualLinkEpisodeId",
                table: "LocalFiles");

            migrationBuilder.DropColumn(
                name: "ManualLinkEpisodeId",
                table: "LocalFiles");

            migrationBuilder.RenameColumn(
                name: "AniDbFileId",
                table: "HangingEpisodeFileXrefs",
                newName: "AniDbNormalFileId");

            migrationBuilder.RenameIndex(
                name: "IX_HangingEpisodeFileXrefs_AniDbFileId",
                table: "HangingEpisodeFileXrefs",
                newName: "IX_HangingEpisodeFileXrefs_AniDbNormalFileId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "FileVersion",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "FileSize",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Ed2k",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "Deprecated",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "Chaptered",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AniDbFiles",
                type: "TEXT",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_AniDbFileId",
                table: "LocalFiles",
                column: "AniDbFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_HangingEpisodeFileXrefs_AniDbFiles_AniDbNormalFileId",
                table: "HangingEpisodeFileXrefs",
                column: "AniDbNormalFileId",
                principalTable: "AniDbFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HangingEpisodeFileXrefs_AniDbFiles_AniDbNormalFileId",
                table: "HangingEpisodeFileXrefs");

            migrationBuilder.DropIndex(
                name: "IX_LocalFiles_AniDbFileId",
                table: "LocalFiles");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AniDbFiles");

            migrationBuilder.RenameColumn(
                name: "AniDbNormalFileId",
                table: "HangingEpisodeFileXrefs",
                newName: "AniDbFileId");

            migrationBuilder.RenameIndex(
                name: "IX_HangingEpisodeFileXrefs_AniDbNormalFileId",
                table: "HangingEpisodeFileXrefs",
                newName: "IX_HangingEpisodeFileXrefs_AniDbFileId");

            migrationBuilder.AddColumn<int>(
                name: "ManualLinkEpisodeId",
                table: "LocalFiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FileVersion",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "FileSize",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ed2k",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Deprecated",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Chaptered",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_AniDbFileId",
                table: "LocalFiles",
                column: "AniDbFileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_ManualLinkEpisodeId",
                table: "LocalFiles",
                column: "ManualLinkEpisodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_HangingEpisodeFileXrefs_AniDbFiles_AniDbFileId",
                table: "HangingEpisodeFileXrefs",
                column: "AniDbFileId",
                principalTable: "AniDbFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalFiles_AniDbEpisodes_ManualLinkEpisodeId",
                table: "LocalFiles",
                column: "ManualLinkEpisodeId",
                principalTable: "AniDbEpisodes",
                principalColumn: "Id");
        }
    }
}
