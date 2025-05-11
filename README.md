You only need to have docker and docker compose installed on your computer.

Once you have docker you copy this repository locally. 
You can use git clone, or simply download the .zip and
then extract.

Once you have the repository locally run:
    docker compose run --rm rocky_env
In the "apk_wow" directory.
This will create the docker image, which you can then use
to modify the apk.

Now to run the container, in the "apk_wow" directory run:
    docker compose run --rm rocky_env
and exit the container by running:
    exit

While the container is running, you can these 
commands from the tools I've made:
    (1) init
        This runs both the download and extract.
        This runs by default when you start
        the container.

    (2) init download
        This only downloads an unmodified XAPK form apkpure, 
        if you don't already have one in your directory.

    (3) init extract
        This generates a folder "unmodified" with the binaries, disassembleda
        and decompiled source code of the XAPK in your folder.
        Useful for breaking down control flow and deciding where and what to
        modify.

    (4) modify
        Generates an XAPK with the modifications you implemented in "mods.cs"
        using monocecil.

    (5) mod
        Alias for modify.

If you need some inspiration or help getting started on writing modifications using
monocecil, checkout he fork "rock-wow/mods.cs". There you see how I implemented
my modifications.

The modifications are all targeted at the Assembly_CSharp.dll, which contains the games
core logic. If you want to expand this you'll have to extend or rewrite my tools.
But for modifying the apk, this will cover almost anything you'd want to modify.

If anything still is unclear check out the repositories discussions or under my reddit post in
the world of warriors community. For any inquiries or issues related to this repository please
post on these public threads, so other people can also benefit from it.

For personal inquiries you can reach me:
    Discord: mario.sc
    Reddit: u/so_arrogant
    