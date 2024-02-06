﻿// <auto-generated />
using System;
using IBS.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240205074454_ChangeTheNameOfFieldsInFuels")]
    partial class ChangeTheNameOfFieldsInFuels
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("IBS.Models.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DisplayOrder")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("IBS.Models.ChartOfAccount", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Category")
                        .HasColumnType("varchar(6)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsMain")
                        .HasColumnType("boolean");

                    b.Property<int>("Level")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Number")
                        .HasColumnType("varchar(15)");

                    b.Property<string>("Parent")
                        .HasColumnType("varchar(15)");

                    b.Property<string>("Type")
                        .HasColumnType("varchar(15)");

                    b.HasKey("Id");

                    b.ToTable("ChartOfAccounts");
                });

            modelBuilder.Entity("IBS.Models.Company", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.Property<string>("BusinessStyle")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<int>("Code")
                        .HasColumnType("integer");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EditedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("EditedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<string>("TinNo")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.HasKey("Id");

                    b.ToTable("Companies");
                });

            modelBuilder.Entity("IBS.Models.Customer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.Property<string>("BusinessStyle")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<int>("Code")
                        .HasColumnType("integer");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CustomerType")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.Property<string>("EditedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("EditedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Terms")
                        .IsRequired()
                        .HasColumnType("varchar(3)");

                    b.Property<string>("TinNo")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<bool>("WithHoldingTax")
                        .HasColumnType("boolean");

                    b.Property<bool>("WithHoldingVat")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("Customers");
                });

            modelBuilder.Entity("IBS.Models.Fuel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric");

                    b.Property<decimal>("AmountDB")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Calibration")
                        .HasColumnType("numeric");

                    b.Property<string>("CanceledBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("CanceledDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<double>("Closing")
                        .HasColumnType("double precision");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EditedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("EditedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<TimeOnly>("End")
                        .HasColumnType("time without time zone");

                    b.Property<DateTime>("INV_DATE")
                        .HasColumnType("date");

                    b.Property<TimeOnly>("InTime")
                        .HasColumnType("time without time zone");

                    b.Property<string>("ItemCode")
                        .IsRequired()
                        .HasColumnType("varchar(16)");

                    b.Property<double>("Liters")
                        .HasColumnType("double precision");

                    b.Property<double>("Opening")
                        .HasColumnType("double precision");

                    b.Property<TimeOnly>("OutTime")
                        .HasColumnType("time without time zone");

                    b.Property<string>("Particulars")
                        .IsRequired()
                        .HasColumnType("varchar(32)");

                    b.Property<string>("PostedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("PostedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric(18,2)");

                    b.Property<int>("Shift")
                        .HasColumnType("integer");

                    b.Property<TimeOnly>("Start")
                        .HasColumnType("time without time zone");

                    b.Property<int>("TransCount")
                        .HasColumnType("integer");

                    b.Property<string>("VoidedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("VoidedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<double>("Volume")
                        .HasColumnType("double precision");

                    b.Property<string>("nozdown")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<int>("xCORPCODE")
                        .HasColumnType("integer");

                    b.Property<int>("xDAY")
                        .HasColumnType("integer");

                    b.Property<int>("xMONTH")
                        .HasColumnType("integer");

                    b.Property<int>("xNOZZLE")
                        .HasColumnType("integer");

                    b.Property<string>("xOID")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("xONAME")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<int>("xPUMP")
                        .HasColumnType("integer");

                    b.Property<int>("xSITECODE")
                        .HasColumnType("integer");

                    b.Property<int>("xTANK")
                        .HasColumnType("integer");

                    b.Property<int>("xTRANSACTION")
                        .HasColumnType("integer");

                    b.Property<int>("xYEAR")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Fuels");
                });

            modelBuilder.Entity("IBS.Models.Lube", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric(18,2)");

                    b.Property<decimal>("AmountDB")
                        .HasColumnType("numeric(18,2)");

                    b.Property<string>("CanceledBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("CanceledDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CashierId")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.Property<string>("CashierName")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<int>("CorpCode")
                        .HasColumnType("integer");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DatetimeStamp")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.Property<int>("Day")
                        .HasColumnType("integer");

                    b.Property<string>("EditedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("EditedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("InvoiceDate")
                        .HasColumnType("date");

                    b.Property<string>("ItemCode")
                        .IsRequired()
                        .HasColumnType("varchar(16)");

                    b.Property<int>("Month")
                        .HasColumnType("integer");

                    b.Property<string>("Particulars")
                        .IsRequired()
                        .HasColumnType("varchar(32)");

                    b.Property<string>("PostedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("PostedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric(18,2)");

                    b.Property<int>("Shift")
                        .HasColumnType("integer");

                    b.Property<int>("SiteCode")
                        .HasColumnType("integer");

                    b.Property<long>("Transaction")
                        .HasColumnType("bigint");

                    b.Property<string>("VoidedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("VoidedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("Volume")
                        .HasColumnType("numeric(18,2)");

                    b.Property<int>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Lubes");
                });

            modelBuilder.Entity("IBS.Models.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EditedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("EditedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Unit")
                        .IsRequired()
                        .HasColumnType("varchar(2)");

                    b.HasKey("Id");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("IBS.Models.SafeDrop", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric(18,2)");

                    b.Property<DateTime>("BDate")
                        .HasColumnType("date");

                    b.Property<string>("CanceledBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("CanceledDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CashierId")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.Property<string>("CashierName")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<int>("CorpCode")
                        .HasColumnType("integer");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("DateTimeStamp")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Day")
                        .HasColumnType("integer");

                    b.Property<string>("EditedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("EditedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("InvoiceDate")
                        .HasColumnType("date");

                    b.Property<int>("Month")
                        .HasColumnType("integer");

                    b.Property<string>("PostedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("PostedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Shift")
                        .HasColumnType("integer");

                    b.Property<int>("SiteCode")
                        .HasColumnType("integer");

                    b.Property<DateTime>("TransactionTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("VoidedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime?>("VoidedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("SafeDrops");
                });

            modelBuilder.Entity("IBS.Models.Supplier", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.Property<int>("Code")
                        .HasColumnType("integer");

                    b.Property<string>("EditedBy")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("EditedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<string>("ProofOfExemptionFilePath")
                        .HasColumnType("varchar(200)");

                    b.Property<string>("ProofOfRegistrationFilePath")
                        .HasColumnType("varchar(200)");

                    b.Property<string>("ReasonOfExemption")
                        .HasColumnType("varchar(100)");

                    b.Property<string>("TaxType")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Terms")
                        .IsRequired()
                        .HasColumnType("varchar(3)");

                    b.Property<string>("TinNo")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Validity")
                        .HasColumnType("varchar(20)");

                    b.Property<DateTime?>("ValidityDate")
                        .HasColumnType("date");

                    b.Property<string>("VatType")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.HasKey("Id");

                    b.ToTable("Suppliers");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(21)
                        .HasColumnType("character varying(21)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);

                    b.HasDiscriminator<string>("Discriminator").HasValue("IdentityUser");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .HasColumnType("text");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("IBS.Models.ApplicationUser", b =>
                {
                    b.HasBaseType("Microsoft.AspNetCore.Identity.IdentityUser");

                    b.Property<string>("Department")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue("ApplicationUser");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
