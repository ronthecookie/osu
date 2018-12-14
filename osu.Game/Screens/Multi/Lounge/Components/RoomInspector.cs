// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens.Multi.Components;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Multi.Lounge.Components
{
    public class RoomInspector : Container
    {
        private const float transition_duration = 100;

        public readonly Bindable<Room> Room = new Bindable<Room>();

        private readonly MarginPadding contentPadding = new MarginPadding { Horizontal = 20, Vertical = 10 };
        private readonly Bindable<string> nameBind = new Bindable<string>();
        private readonly Bindable<User> hostBind = new Bindable<User>();
        private readonly Bindable<RoomStatus> statusBind = new Bindable<RoomStatus>();
        private readonly Bindable<GameType> typeBind = new Bindable<GameType>();
        private readonly Bindable<int?> maxParticipantsBind = new Bindable<int?>();
        private readonly Bindable<IEnumerable<User>> participantsBind = new Bindable<IEnumerable<User>>();
        private readonly IBindableCollection<PlaylistItem> playlistBind = new BindableCollection<PlaylistItem>();

        private readonly Bindable<WorkingBeatmap> beatmap = new Bindable<WorkingBeatmap>();

        private OsuColour colours;
        private Box statusStrip;
        private UpdateableBeatmapBackgroundSprite background;
        private ParticipantCount participantCount;
        private FillFlowContainer topFlow, participantsFlow;
        private OsuSpriteText name, status;
        private BeatmapTypeInfo beatmapTypeInfo;
        private ScrollContainer participantsScroll;
        private ParticipantInfo participantInfo;

        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            this.colours = colours;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = OsuColour.FromHex(@"343138"),
                },
                topFlow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 200,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                background = new UpdateableBeatmapBackgroundSprite { RelativeSizeAxes = Axes.Both },
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0.5f), Color4.Black.Opacity(0)),
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(20),
                                    Children = new Drawable[]
                                    {
                                        participantCount = new ParticipantCount
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                        },
                                        name = new OsuSpriteText
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            TextSize = 30,
                                        },
                                    },
                                },
                            },
                        },
                        statusStrip = new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 5,
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = OsuColour.FromHex(@"28242d"),
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    LayoutDuration = transition_duration,
                                    Padding = contentPadding,
                                    Spacing = new Vector2(0f, 5f),
                                    Children = new Drawable[]
                                    {
                                        status = new OsuSpriteText
                                        {
                                            TextSize = 14,
                                            Font = @"Exo2.0-Bold",
                                        },
                                        beatmapTypeInfo = new BeatmapTypeInfo(),
                                    },
                                },
                            },
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = contentPadding,
                            Children = new Drawable[]
                            {
                                participantInfo = new ParticipantInfo(@"Rank Range "),
                            },
                        },
                    },
                },
                participantsScroll = new OsuScrollContainer
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativeSizeAxes = Axes.X,
                    Padding = new MarginPadding { Top = contentPadding.Top, Left = 38, Right = 37 },
                    Children = new[]
                    {
                        participantsFlow = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            LayoutDuration = transition_duration,
                            Spacing = new Vector2(5f),
                        },
                    },
                },
            };

            playlistBind.ItemsAdded += _ => updatePlaylist();
            playlistBind.ItemsRemoved += _ => updatePlaylist();

            statusBind.BindValueChanged(displayStatus);
            participantsBind.BindValueChanged(p => participantsFlow.ChildrenEnumerable = p.Select(u => new UserTile(u)));

            nameBind.BindValueChanged(n => name.Text = n);

            participantInfo.Host.BindTo(hostBind);
            participantInfo.Participants.BindTo(participantsBind);

            participantCount.Participants.BindTo(participantsBind);
            participantCount.MaxParticipants.BindTo(maxParticipantsBind);

            beatmapTypeInfo.Type.BindTo(typeBind);

            Room.BindValueChanged(updateRoom, true);
        }

        private Room lastRoom;

        private void updateRoom(Room newRoom)
        {
            if (lastRoom != null)
            {
                nameBind.UnbindFrom(lastRoom.Name);
                hostBind.UnbindFrom(lastRoom.Host);
                statusBind.UnbindFrom(lastRoom.Status);
                typeBind.UnbindFrom(lastRoom.Type);
                playlistBind.UnbindFrom(lastRoom.Playlist);
                maxParticipantsBind.UnbindFrom(lastRoom.MaxParticipants);
                participantsBind.UnbindFrom(lastRoom.Participants);
            }

            if (newRoom != null)
            {
                nameBind.BindTo(newRoom.Name);
                hostBind.BindTo(newRoom.Host);
                statusBind.BindTo(newRoom.Status);
                typeBind.BindTo(newRoom.Type);
                playlistBind.BindTo(newRoom.Playlist);
                maxParticipantsBind.BindTo(newRoom.MaxParticipants);
                participantsBind.BindTo(newRoom.Participants);

                participantsFlow.FadeIn(transition_duration);
                participantCount.FadeIn(transition_duration);
                beatmapTypeInfo.FadeIn(transition_duration);
                name.FadeIn(transition_duration);
                participantInfo.FadeIn(transition_duration);
            }
            else
            {
                participantsFlow.FadeOut(transition_duration);
                participantCount.FadeOut(transition_duration);
                beatmapTypeInfo.FadeOut(transition_duration);
                name.FadeOut(transition_duration);
                participantInfo.FadeOut(transition_duration);

                displayStatus(new RoomStatusNoneSelected());
            }

            lastRoom = newRoom;
        }

        private void updatePlaylist()
        {
            if (playlistBind.Count == 0)
                return;

            // For now, only the first playlist item is supported
            var item = playlistBind.First();

            beatmap.Value = beatmaps.GetWorkingBeatmap(item.Beatmap);
            background.Beatmap.Value = item.Beatmap;
            beatmapTypeInfo.Beatmap.Value = item.Beatmap;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            participantsScroll.Height = DrawHeight - topFlow.DrawHeight;
        }

        private void displayStatus(RoomStatus s)
        {
            status.Text = s.Message;

            Color4 c = s.GetAppropriateColour(colours);
            statusStrip.FadeColour(c, transition_duration);
            status.FadeColour(c, transition_duration);
        }

        private class UserTile : Container, IHasTooltip
        {
            private readonly User user;

            public string TooltipText => user.Username;

            public UserTile(User user)
            {
                this.user = user;
                Size = new Vector2(70f);
                CornerRadius = 5f;
                Masking = true;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.FromHex(@"27252d"),
                    },
                    new UpdateableAvatar
                    {
                        RelativeSizeAxes = Axes.Both,
                        User = user,
                    },
                };
            }
        }

        private class RoomStatusNoneSelected : RoomStatus
        {
            public override string Message => @"No Room Selected";
            public override Color4 GetAppropriateColour(OsuColour colours) => colours.Gray8;
        }
    }
}
