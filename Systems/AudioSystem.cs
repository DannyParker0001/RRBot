﻿namespace RRBot.Systems;
public sealed class AudioSystem
{
    private readonly IAudioService audioService;
    private readonly DiscordSocketClient client;

    public AudioSystem(IAudioService audioService, DiscordSocketClient client)
    {
        this.audioService = audioService;
        this.client = client;
    }

    public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, float volume)
    {
        if (volume < Constants.MIN_VOLUME || volume > Constants.MAX_VOLUME)
            return CommandResult.FromError($"Volume must be between {Constants.MIN_VOLUME}% and {Constants.MAX_VOLUME}%.");
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        await player.SetVolumeAsync(volume / 100f, true);
        await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        LavalinkTrack track = player.CurrentTrack;
        StringBuilder builder = new($"By: {RRFormat.Sanitize(track.Author)}\n");
        if (!track.IsLiveStream)
            builder.AppendLine($"Duration: {track.Duration}");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(track.Title)
            .WithDescription(builder.ToString());
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> GetLyricsAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);

        using LyricsService lyricsService = new(new());
        string lyrics = await lyricsService.RequestLyricsAsync(player.CurrentTrack.Author, player.CurrentTrack.Title);
        if (string.IsNullOrWhiteSpace(lyrics))
            return CommandResult.FromError("No lyrics found!");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"{player.CurrentTrack.Title} Lyrics")
            .WithDescription(RRFormat.Sanitize(lyrics));
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);

        if (player.Queue.IsEmpty)
        {
            await context.Channel.SendMessageAsync($"Now playing: \"{RRFormat.Sanitize(player.CurrentTrack.Title)}\". Nothing else is queued.");
            return CommandResult.FromSuccess();
        }

        StringBuilder playlist = new($"**1**: \"{RRFormat.Sanitize(player.CurrentTrack.Title)}\" by {RRFormat.Sanitize(player.CurrentTrack.Author)} {(!player.CurrentTrack.IsLiveStream ? $"({player.CurrentTrack.Duration})\n" : "\n")}");
        for (int i = 0; i < player.Queue.Count; i++)
        {
            LavalinkTrack track = player.Queue[i];
            playlist.AppendLine($"**{i + 2}**: \"{RRFormat.Sanitize(track.Title)}\" by {RRFormat.Sanitize(track.Author)} {(!track.IsLiveStream ? $"({track.Duration})" : "")}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Playlist")
            .WithDescription(playlist.ToString());
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> LoopAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        player.IsLooping = !player.IsLooping;
        await context.Channel.SendMessageAsync($"Looping turned {(player.IsLooping ? "ON" : "OFF")}.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> PlayAsync(SocketCommandContext context, string query)
    {
        SocketGuildUser user = context.User as SocketGuildUser;
        if (user.VoiceChannel is null)
            return CommandResult.FromError("You must be in a voice channel.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild.Id)
            ?? await audioService.JoinAsync<VoteLavalinkPlayer>(context.Guild.Id, user.VoiceChannel.Id, true);

        LavalinkTrack track = await audioService.GetTrackAsync(query, SearchMode.YouTube);
        if (track is null)
            return CommandResult.FromError("No results were found for your query.");
        if (!track.IsLiveStream && track.Duration.TotalSeconds > 7200)
            return CommandResult.FromError("This is too long for me to play! It must be 2 hours or shorter in length.");

        int position = await player.PlayAsync(track, enqueue: true);
        if (position == 0)
        {
            StringBuilder message = new($"Now playing: \"{RRFormat.Sanitize(track.Title)}\"\nBy: {RRFormat.Sanitize(track.Author)}\n");
            if (!track.IsLiveStream)
                message.AppendLine($"Length: {track.Duration}");
            message.AppendLine("*Tip: if the track instantly doesn't play, it's probably age restricted.*");
            await context.Channel.SendMessageAsync(message.ToString());
        }
        else
        {
            await context.Channel.SendMessageAsync($"**{RRFormat.Sanitize(track.Title)}** has been added to the queue.");
        }

        await LoggingSystem.Custom_TrackStarted(user, track.Source);
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ShuffleAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        if (player.Queue.Count <= 1)
            return CommandResult.FromError("There must be at least 2 tracks in the queue to shuffle.");

        player.Queue.Shuffle();
        await context.Channel.SendMessageAsync("Shuffled the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        await context.Channel.SendMessageAsync($"Skipped \"{RRFormat.Sanitize(player.CurrentTrack.Title)}\".");
        if (!player.Queue.TryDequeue(out LavalinkTrack track))
        {
            await player.StopAsync(true);
        }
        else
        {
            await player.SkipAsync();
            await player.PlayAsync(track);
        }

        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> StopAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        await player.StopAsync(true);
        await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> VoteSkipTrackAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        string track = RRFormat.Sanitize(player.CurrentTrack.Title);
        UserVoteSkipInfo info = await player.VoteAsync(context.User.Id);
        if (!info.WasAdded)
            return CommandResult.FromError("You already voted to skip!");

        int votesNeeded = (int)Math.Ceiling((double)info.TotalUsers / 2) - info.Votes.Count;
        if (votesNeeded > 0)
        {
            await context.Channel.SendMessageAsync($"Vote received! **{votesNeeded}** more votes are needed.");
        }
        else
        {
            await context.Channel.SendMessageAsync($"Skipped \"{track}\".");
        }

        return CommandResult.FromSuccess();
    }
}