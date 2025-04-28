(1) I simply want to play the game, how do I obtain its XAPK:
        This link: <URL> should contain the newest version. If the link doesn't
        make a post on the repositories discussion or under the reddit post.
        You can also build it yourself by setting up the environment as
        described below and then building the XAPK.

(2) I want to modify the game and create a new 
        Set up the environment as described in    

(3) How do I set up the enviromnent?
        Make sure you have docker, docker decompose and git installed on
        your shell.
        Then fork this repository on github and clone it locally.
        Then you simply build the docker image with:
            docker compose build rocky_env

(4) How do I work and generate my modifications?
        Once the docker image is created ad in (3) 
        you can create and run a container with:
            docker compose run --rm rocky_env

        and exit the container by typing:
            exit

        Once you're inside the container you can use following commands:
            (1) init
            (2) init download
            (3) init extract
            (4) modify
            (5) mod
        If you're missing an XAPK in you're working dir, "init download" 
        will download one from APKpure with.
        You need an XAPK in you're working directory for "mod" or "modify" to work, you can also
        simply place it there manually.

        To get a folder named "unmodified" with the decompiled and dissambled source form the XAPK in you're working directory run "init extract".
        If your run "init" it will first download then extract.

        If you run "modify" or "mod" they will generate the modified XAPK from the modifications you've implemented in "mods.cs". Make sure to already have a XAPK in your working directory and they're aliases.

        To implement the modifications in mods.cs, monocecil is used to modify the the CIL
        instructions in the target .dll file (Assembly-CSharp.dll by default) to change this you'll have to change the modify script in "tools/modify.sh". 
        
        Keep in mind that for most cases you won't have to modify any other .dll or content in the APK as the games defining logic is purely contained in this single .dll. If you wish contribute by improving the tools or layout in general feel free to do so and send a pull-request.
        However keep in mind that the goal is to offer as much power in generating modifications
        whilst also keeping the workflow as simple as possible.

        Check out this youtube video for a more thorough guide: link

If anything still is unclear check out the repositories discussions or under my reddit post in
the world of warriors community. For any inquiries or issues related to this repository please
post on these public threads, so other people can also benefit from it.

For personal inquiries you can reach me:
    Discord: mario.sc
    Reddit: u/so_arrogant
    