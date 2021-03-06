﻿
using System;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Diagnostics;
using Smrf.AppLib;
using Smrf.XmlLib;
using Smrf.DateTimeLib;
using Smrf.SocialNetworkLib;
using Smrf.SocialNetworkLib.Twitter;
using Smrf.NodeXL.GraphMLLib;

namespace Smrf.NodeXL.GraphDataProviders.Twitter
{
//*****************************************************************************
//  Class: TwitterSearchNetworkAnalyzer
//
/// <summary>
/// Gets a network of people who have tweeted a specified search term.
/// </summary>
///
/// <remarks>
/// Use <see cref="GetNetworkAsync" /> to asynchronously get the network, or
/// <see cref="GetNetwork" /> to get it synchronously.
/// </remarks>
//*****************************************************************************

public class TwitterSearchNetworkAnalyzer : TwitterNetworkAnalyzerBase
{
    //*************************************************************************
    //  Constructor: TwitterSearchNetworkAnalyzer()
    //
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="TwitterSearchNetworkAnalyzer" /> class.
    /// </summary>
    //*************************************************************************

    public TwitterSearchNetworkAnalyzer()
    {
        // (Do nothing.)

        AssertValid();
    }

    //*************************************************************************
    //  Enum: WhatToInclude
    //
    /// <summary>
    /// Flags that specify what should be included in a network requested from
    /// this class.
    /// </summary>
    ///
    /// <remarks>
    /// The flags can be ORed together.
    /// </remarks>
    //*************************************************************************

    [System.FlagsAttribute]

    public enum
    WhatToInclude
    {
        /// <summary>
        /// Include nothing.
        /// </summary>

        None = 0,

        /// <summary>
        /// Expand the URLs contained within each status.
        /// </summary>

        ExpandedStatusUrls = 1,

        /// <summary>
        /// Include an edge for each followed relationship.
        /// </summary>

        FollowedEdges = 2,
    }

    //*************************************************************************
    //  Method: GetNetworkAsync()
    //
    /// <summary>
    /// Asynchronously gets a directed network of people who have tweeted a
    /// specified search term.
    /// </summary>
    ///
    /// <param name="searchTerm">
    /// The term to search for.
    /// </param>
    ///
    /// <param name="whatToInclude">
    /// Specifies what should be included in the network.
    /// </param>
    ///
    /// <param name="maximumStatuses">
    /// Maximum number of tweets to request.  Can't be Int32.MaxValue.
    /// </param>
    ///
    /// <remarks>
    /// When the analysis completes, the <see
    /// cref="HttpNetworkAnalyzerBase.AnalysisCompleted" /> event fires.  The
    /// <see cref="RunWorkerCompletedEventArgs.Result" /> property will return
    /// an XmlDocument containing the network as GraphML.
    ///
    /// <para>
    /// To cancel the analysis, call <see
    /// cref="HttpNetworkAnalyzerBase.CancelAsync" />.
    /// </para>
    ///
    /// </remarks>
    //*************************************************************************

    public void
    GetNetworkAsync
    (
        String searchTerm,
        WhatToInclude whatToInclude,
        Int32 maximumStatuses
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(searchTerm) );
        Debug.Assert(maximumStatuses > 0);
        Debug.Assert(maximumStatuses != Int32.MaxValue);
        AssertValid();

        const String MethodName = "GetNetworkAsync";
        CheckIsBusy(MethodName);

        // Wrap the arguments in an object that can be passed to
        // BackgroundWorker.RunWorkerAsync().

        GetNetworkAsyncArgs oGetNetworkAsyncArgs = new GetNetworkAsyncArgs();

        oGetNetworkAsyncArgs.SearchTerm = searchTerm;
        oGetNetworkAsyncArgs.WhatToInclude = whatToInclude;
        oGetNetworkAsyncArgs.MaximumStatuses = maximumStatuses;

        m_oBackgroundWorker.RunWorkerAsync(oGetNetworkAsyncArgs);
    }

    //*************************************************************************
    //  Method: GetNetwork()
    //
    /// <summary>
    /// Synchronously gets a directed network of people who have tweeted a
    /// specified search term.
    /// </summary>
    ///
    /// <param name="searchTerm">
    /// The term to search for.
    /// </param>
    ///
    /// <param name="whatToInclude">
    /// Specifies what should be included in the network.
    /// </param>
    ///
    /// <param name="maximumStatuses">
    /// Maximum number of tweets to request.  Can't be Int32.MaxValue.
    /// </param>
    ///
    /// <returns>
    /// An XmlDocument containing the network as GraphML.
    /// </returns>
    //*************************************************************************

    public XmlDocument
    GetNetwork
    (
        String searchTerm,
        WhatToInclude whatToInclude,
        Int32 maximumStatuses
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(searchTerm) );
        Debug.Assert(maximumStatuses > 0);
        Debug.Assert(maximumStatuses != Int32.MaxValue);
        AssertValid();

        return ( GetNetworkInternal(searchTerm, whatToInclude,
            maximumStatuses) );
    }

    //*************************************************************************
    //  Method: GetNetworkInternal()
    //
    /// <overloads>
    /// Gets the requested network.
    /// </overloads>
    ///
    /// <summary>
    /// Gets the requested network.
    /// </summary>
    ///
    /// <param name="sSearchTerm">
    /// The term to search for.
    /// </param>
    ///
    /// <param name="eWhatToInclude">
    /// Specifies what should be included in the network.
    /// </param>
    ///
    /// <param name="iMaximumStatuses">
    /// Maximum number of tweets to request.  Can't be Int32.MaxValue.
    /// </param>
    ///
    /// <returns>
    /// An XmlDocument containing the network as GraphML.
    /// </returns>
    //*************************************************************************

    protected XmlDocument
    GetNetworkInternal
    (
        String sSearchTerm,
        WhatToInclude eWhatToInclude,
        Int32 iMaximumStatuses
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sSearchTerm) );
        Debug.Assert(iMaximumStatuses > 0);
        Debug.Assert(iMaximumStatuses != Int32.MaxValue);
        AssertValid();

        BeforeGetNetwork();

        GraphMLXmlDocument oGraphMLXmlDocument =
            TwitterSearchNetworkGraphMLUtil.CreateGraphMLXmlDocument();

        RequestStatistics oRequestStatistics = new RequestStatistics();

        try
        {
            GetNetworkInternal(sSearchTerm, eWhatToInclude, iMaximumStatuses,
                oRequestStatistics, oGraphMLXmlDocument);
        }
        catch (Exception oException)
        {
            OnUnexpectedException(oException, oGraphMLXmlDocument,
                oRequestStatistics);
        }

        OnNetworkObtained(oGraphMLXmlDocument, oRequestStatistics, 

            GetNetworkDescription(sSearchTerm, eWhatToInclude,
                iMaximumStatuses, oGraphMLXmlDocument),

            SnaTitleCreator.CreateSnaTitle(sSearchTerm, oRequestStatistics),
            "Twitter Search " + sSearchTerm
            );

        return (oGraphMLXmlDocument);
    }

    //*************************************************************************
    //  Method: GetNetworkInternal()
    //
    /// <summary>
    /// Gets the requested network, given a GraphMLXmlDocument.
    /// </summary>
    ///
    /// <param name="sSearchTerm">
    /// The term to search for.
    /// </param>
    ///
    /// <param name="eWhatToInclude">
    /// Specifies what should be included in the network.
    /// </param>
    ///
    /// <param name="iMaximumStatuses">
    /// Maximum number of tweets to request.  Can't be Int32.MaxValue.
    /// </param>
    ///
    /// <param name="oRequestStatistics">
    /// A <see cref="RequestStatistics" /> object that is keeping track of
    /// requests made while getting the network.
    /// </param>
    ///
    /// <param name="oGraphMLXmlDocument">
    /// The GraphMLXmlDocument to populate with the requested network.
    /// </param>
    //*************************************************************************

    protected void
    GetNetworkInternal
    (
        String sSearchTerm,
        WhatToInclude eWhatToInclude,
        Int32 iMaximumStatuses,
        RequestStatistics oRequestStatistics,
        GraphMLXmlDocument oGraphMLXmlDocument
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sSearchTerm) );
        Debug.Assert(iMaximumStatuses > 0);
        Debug.Assert(iMaximumStatuses != Int32.MaxValue);
        Debug.Assert(oRequestStatistics != null);
        Debug.Assert(oGraphMLXmlDocument != null);
        AssertValid();

        // The key is the Twitter user ID and the value is the corresponding
        // TwitterUser.

        Dictionary<String, TwitterUser> oUserIDDictionary =
            new Dictionary<String, TwitterUser>();

        // First, append a vertex for each person who has tweeted the search
        // term.

        AppendVertexXmlNodesForSearchTerm(sSearchTerm, eWhatToInclude,
            iMaximumStatuses, oGraphMLXmlDocument, oUserIDDictionary,
            oRequestStatistics);

        // Now append a vertex for each person who was mentioned or replied to
        // by the first set of people, but who didn't tweet the search term
        // himself.

        AppendVertexXmlNodesForMentionsAndRepliesTo(eWhatToInclude,
            oGraphMLXmlDocument, oUserIDDictionary, oRequestStatistics);

        TwitterSearchNetworkGraphMLUtil.AppendVertexTooltipXmlNodes(
            oGraphMLXmlDocument, oUserIDDictionary);

        if ( WhatToIncludeFlagIsSet(eWhatToInclude,
            WhatToInclude.FollowedEdges) )
        {
            // Look at each author's friends, and if a friend has also tweeted
            // the search term, add an edge between the author and the friend.

            AppendFriendEdgeXmlNodes(oUserIDDictionary, MaximumFriends,
                oGraphMLXmlDocument, oRequestStatistics);
        }

        AppendRepliesToAndMentionsEdgeXmlNodes(oGraphMLXmlDocument,
            oUserIDDictionary.Values,

            TwitterGraphMLUtil.TwitterUsersToUniqueScreenNames(
                oUserIDDictionary.Values)
            );
    }

    //*************************************************************************
    //  Method: AppendVertexXmlNodesForSearchTerm()
    //
    /// <summary>
    /// Appends a vertex XML node for each person who has tweeted a specified
    /// search term.
    /// </summary>
    ///
    /// <param name="sSearchTerm">
    /// The term to search for.
    /// </param>
    ///
    /// <param name="eWhatToInclude">
    /// Specifies what should be included in the network.
    /// </param>
    ///
    /// <param name="iMaximumStatuses">
    /// Maximum number of tweets to request.  Can't be Int32.MaxValue.
    /// </param>
    ///
    /// <param name="oGraphMLXmlDocument">
    /// The GraphMLXmlDocument being populated.
    /// </param>
    ///
    /// <param name="oUserIDDictionary">
    /// The key is the Twitter user ID and the value is the corresponding
    /// TwitterUser.  The dictionary is empty when this method is called, and
    /// this method populates the dictionary.
    /// </param>
    ///
    /// <param name="oRequestStatistics">
    /// A <see cref="RequestStatistics" /> object that is keeping track of
    /// requests made while getting the network.
    /// </param>
    //*************************************************************************

    protected void
    AppendVertexXmlNodesForSearchTerm
    (
        String sSearchTerm,
        WhatToInclude eWhatToInclude,
        Int32 iMaximumStatuses,
        GraphMLXmlDocument oGraphMLXmlDocument,
        Dictionary<String, TwitterUser> oUserIDDictionary,
        RequestStatistics oRequestStatistics
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sSearchTerm) );
        Debug.Assert(iMaximumStatuses > 0);
        Debug.Assert(iMaximumStatuses != Int32.MaxValue);
        Debug.Assert(oGraphMLXmlDocument != null);
        Debug.Assert(oUserIDDictionary != null);
        Debug.Assert(oRequestStatistics != null);
        AssertValid();

        Boolean bExpandStatusUrls = WhatToIncludeFlagIsSet(
            eWhatToInclude, WhatToInclude.ExpandedStatusUrls);

        ReportProgress("Getting a list of tweets.");

        // Get the tweets that contain the search term.  Note that multiple
        // tweets may have the same author.

        Debug.Assert(m_oTwitterUtil != null);

        foreach ( Dictionary<String, Object> oStatusValueDictionary in

            m_oTwitterUtil.EnumerateSearchStatuses(
                sSearchTerm, iMaximumStatuses, oRequestStatistics,
                new ReportProgressHandler(this.ReportProgress),

                new CheckCancellationPendingHandler(
                    this.CheckCancellationPending)
                ) )
        {
            const String UserKeyName = "user";

            if ( !oStatusValueDictionary.ContainsKey(UserKeyName) )
            {
                // This has actually happened--Twitter occasionally sends a
                // status without user information.

                continue;
            }

            Dictionary<String, Object> oUserValueDictionary =
                ( Dictionary<String, Object> )
                oStatusValueDictionary[UserKeyName];

            TwitterUser oTwitterUser;

            if ( !TwitterSearchNetworkGraphMLUtil.
                TryAppendVertexXmlNode(oUserValueDictionary, true,
                    oGraphMLXmlDocument, oUserIDDictionary, out oTwitterUser) )
            {
                continue;
            }

            // Parse the status and add it to the user's status collection.

            if ( !TwitterSearchNetworkGraphMLUtil.TryAddStatusToUser(
                oStatusValueDictionary, oTwitterUser, bExpandStatusUrls) )
            {
                continue;
            }
        }
    }

    //*************************************************************************
    //  Method: AppendVertexXmlNodesForMentionsAndRepliesTo()
    //
    /// <summary>
    /// Appends a vertex XML node for each person who was mentioned or replied
    /// to but who didn't tweet the search term himself.
    /// </summary>
    ///
    /// <param name="eWhatToInclude">
    /// Specifies what should be included in the network.
    /// </param>
    ///
    /// <param name="oGraphMLXmlDocument">
    /// The GraphMLXmlDocument being populated.
    /// </param>
    ///
    /// <param name="oUserIDDictionary">
    /// The key is the Twitter user ID and the value is the corresponding
    /// TwitterUser.  The dictionary is populated with users who tweeted the
    /// search term when this method is called, and this method adds more
    /// users.
    /// </param>
    ///
    /// <param name="oRequestStatistics">
    /// A <see cref="RequestStatistics" /> object that is keeping track of
    /// requests made while getting the network.
    /// </param>
    //*************************************************************************

    protected void
    AppendVertexXmlNodesForMentionsAndRepliesTo
    (
        WhatToInclude eWhatToInclude,
        GraphMLXmlDocument oGraphMLXmlDocument,
        Dictionary<String, TwitterUser> oUserIDDictionary,
        RequestStatistics oRequestStatistics
    )
    {
        Debug.Assert(oGraphMLXmlDocument != null);
        Debug.Assert(oUserIDDictionary != null);
        Debug.Assert(oRequestStatistics != null);
        AssertValid();

        ReportProgress("Getting replied to and mentioned users.");

        // Get the screen names that were mentioned or replied to by the people
        // who tweeted the search term.

        String[] asUniqueMentionsAndRepliesToScreenNames =
            TwitterSearchNetworkGraphMLUtil.GetMentionsAndRepliesToScreenNames(
                oUserIDDictionary);

        // Get information about each of those screen names and append vertex
        // XML nodes for them if necessary.

        foreach ( Dictionary<String, Object> oUserValueDictionary in
            EnumerateUserValueDictionaries(
                asUniqueMentionsAndRepliesToScreenNames, false,
                oRequestStatistics) )
        {
            TwitterUser oTwitterUser;

            TwitterSearchNetworkGraphMLUtil.TryAppendVertexXmlNode(
                oUserValueDictionary, false, oGraphMLXmlDocument,
                oUserIDDictionary, out oTwitterUser);
        }
    }

    //*************************************************************************
    //  Method: AppendFriendEdgeXmlNodes()
    //
    /// <overloads>
    /// Appends edge XML nodes for friends.
    /// </overloads>
    ///
    /// <summary>
    /// Appends edge XML nodes for the friends in the entire network.
    /// </summary>
    ///
    /// <param name="oUserIDDictionary">
    /// The key is the Twitter user ID and the value is the corresponding
    /// TwitterUser.
    /// </param>
    ///
    /// <param name="iMaximumPeoplePerRequest">
    /// Maximum number of people to request for each query, or Int32.MaxValue
    /// for no limit.
    /// </param>
    ///
    /// <param name="oGraphMLXmlDocument">
    /// GraphMLXmlDocument being populated.
    /// </param>
    ///
    /// <param name="oRequestStatistics">
    /// A <see cref="RequestStatistics" /> object that is keeping track of
    /// requests made while getting the network.
    /// </param>
    //*************************************************************************

    protected void
    AppendFriendEdgeXmlNodes
    (
        Dictionary<String, TwitterUser> oUserIDDictionary,
        Int32 iMaximumPeoplePerRequest,
        GraphMLXmlDocument oGraphMLXmlDocument,
        RequestStatistics oRequestStatistics
    )
    {
        Debug.Assert(oUserIDDictionary != null);
        Debug.Assert(iMaximumPeoplePerRequest > 0);
        Debug.Assert(oGraphMLXmlDocument != null);
        Debug.Assert(oRequestStatistics != null);
        AssertValid();

        AppendFriendEdgeXmlNodes(

            TwitterGraphMLUtil.TwitterUsersToUniqueScreenNames(
                oUserIDDictionary.Values),

            oUserIDDictionary, iMaximumPeoplePerRequest, oGraphMLXmlDocument,
            oRequestStatistics);
    }

    //*************************************************************************
    //  Method: AppendFriendEdgeXmlNodes()
    //
    /// <summary>
    /// Appends edge XML nodes for the friends of a specified collection of
    /// screen names.
    /// </summary>
    ///
    /// <param name="oScreenNames">
    /// Collection of screen names to append edges for.
    /// </param>
    ///
    /// <param name="oUserIDDictionary">
    /// The key is the Twitter user ID and the value is the corresponding
    /// TwitterUser.
    /// </param>
    ///
    /// <param name="iMaximumPeoplePerRequest">
    /// Maximum number of people to request for each query, or Int32.MaxValue
    /// for no limit.
    /// </param>
    ///
    /// <param name="oGraphMLXmlDocument">
    /// GraphMLXmlDocument being populated.
    /// </param>
    ///
    /// <param name="oRequestStatistics">
    /// A <see cref="RequestStatistics" /> object that is keeping track of
    /// requests made while getting the network.
    /// </param>
    //*************************************************************************

    protected void
    AppendFriendEdgeXmlNodes
    (
        ICollection<String> oScreenNames,
        Dictionary<String, TwitterUser> oUserIDDictionary,
        Int32 iMaximumPeoplePerRequest,
        GraphMLXmlDocument oGraphMLXmlDocument,
        RequestStatistics oRequestStatistics
    )
    {
        Debug.Assert(oScreenNames != null);
        Debug.Assert(oUserIDDictionary != null);
        Debug.Assert(iMaximumPeoplePerRequest > 0);
        Debug.Assert(oGraphMLXmlDocument != null);
        Debug.Assert(oRequestStatistics != null);
        AssertValid();

        foreach (String sScreenName in oScreenNames)
        {
            ReportProgressForFriendsOrFollowers(sScreenName, true);

            // We need to find out who are the friends of sScreenName, and see
            // if any of them are in oUserIDDictionary, which means they are in
            // the network.

            foreach ( String sOtherUserID in EnumerateFriendOrFollowerIDs(
                sScreenName, true, iMaximumPeoplePerRequest,
                oRequestStatistics) )
            {
                TwitterUser oOtherTwitterUser;

                if ( oUserIDDictionary.TryGetValue(sOtherUserID,
                    out oOtherTwitterUser) )
                {
                    // oOtherTwitterUser is a friend of sScreenName.

                    AppendFriendOrFollowerEdgeXmlNode(sScreenName,
                        oOtherTwitterUser.ScreenName, true,
                        oGraphMLXmlDocument, oRequestStatistics);
                }
            }
        }
    }

    //*************************************************************************
    //  Method: WhatToIncludeFlagIsSet()
    //
    /// <summary>
    /// Checks whether a flag is set in an ORed combination of WhatToInclude
    /// flags.
    /// </summary>
    ///
    /// <param name="eORedEnumFlags">
    /// Zero or more ORed Enum flags.
    /// </param>
    ///
    /// <param name="eORedEnumFlagsToCheck">
    /// One or more Enum flags to check.
    /// </param>
    ///
    /// <returns>
    /// true if any of the <paramref name="eORedEnumFlagsToCheck" /> flags are
    /// set in <paramref name="eORedEnumFlags" />.
    /// </returns>
    //*************************************************************************

    protected Boolean
    WhatToIncludeFlagIsSet
    (
        WhatToInclude eORedEnumFlags,
        WhatToInclude eORedEnumFlagsToCheck
    )
    {
        return ( (eORedEnumFlags & eORedEnumFlagsToCheck) != 0 );
    }

    //*************************************************************************
    //  Method: GetNetworkDescription()
    //
    /// <summary>
    /// Gets a description of the network.
    /// </summary>
    ///
    /// <param name="sSearchTerm">
    /// The term to search for.
    /// </param>
    ///
    /// <param name="eWhatToInclude">
    /// Specifies what should be included in the network.
    /// </param>
    ///
    /// <param name="iMaximumStatuses">
    /// Maximum number of tweets to request.  Can't be Int32.MaxValue.
    /// </param>
    ///
    /// <param name="oGraphMLXmlDocument">
    /// The GraphMLXmlDocument that contains the network.
    /// </param>
    ///
    /// <returns>
    /// A description of the network.
    /// </returns>
    //*************************************************************************

    protected String
    GetNetworkDescription
    (
        String sSearchTerm,
        WhatToInclude eWhatToInclude,
        Int32 iMaximumStatuses,
        GraphMLXmlDocument oGraphMLXmlDocument
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sSearchTerm) );
        Debug.Assert(iMaximumStatuses > 0);
        Debug.Assert(iMaximumStatuses != Int32.MaxValue);
        Debug.Assert(oGraphMLXmlDocument != null);
        AssertValid();

        const String Int32FormatString = "N0";

        NetworkDescriber oNetworkDescriber = new NetworkDescriber();
        Int32 iVertexXmlNodes = oGraphMLXmlDocument.VertexXmlNodes;

        oNetworkDescriber.AddSentence(

            // WARNING:
            //
            // If you change this first sentence, you may also have to change
            // the code in
            // TwitterSearchNetworkTopItemsCalculator2.GetSearchTerm(), which
            // attempts to extract the search term from the graph description.

            "The graph represents a network of {0} Twitter {1} whose recent"
            + " tweets contained \"{2}\", or who {3} replied to or mentioned"
            + " in those tweets, taken from a data set limited to a maximum of"
            + " {4} tweets."
            ,
            iVertexXmlNodes.ToString(Int32FormatString),
            StringUtil.MakePlural("user", iVertexXmlNodes),
            sSearchTerm,
            iVertexXmlNodes > 1 ? "were" : "was",
            iMaximumStatuses.ToString(Int32FormatString)
            );

        oNetworkDescriber.AddNetworkTime(NetworkSource);

        Boolean bIncludeFollowedEdges = WhatToIncludeFlagIsSet(eWhatToInclude,
            WhatToInclude.FollowedEdges);

        // Every collected tweet has an edge that contains the date of the
        // tweet, so the range of tweet dates can be determined.

        oNetworkDescriber.StartNewParagraph();

        TweetDateRangeAnalyzer.AddTweetDateRangeToNetworkDescription(
            oGraphMLXmlDocument, oNetworkDescriber);

        if (bIncludeFollowedEdges)
        {
            oNetworkDescriber.StartNewParagraph();

            oNetworkDescriber.AddSentence(
                "There is an edge for each friend relationship."
                );
        }

        oNetworkDescriber.AddSentence(
            "There is an edge for each \"replies-to\" relationship in a"
            + " tweet, an edge for each \"mentions\" relationship in a tweet,"
            + " and a self-loop edge for each tweet that is not a"
            + " \"replies-to\" or \"mentions\"."
            );

        return ( oNetworkDescriber.ConcatenateSentences() );
    }

    //*************************************************************************
    //  Method: BackgroundWorker_DoWork()
    //
    /// <summary>
    /// Handles the DoWork event on the BackgroundWorker object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Source of the event.
    /// </param>
    ///
    /// <param name="e">
    /// Standard mouse event arguments.
    /// </param>
    //*************************************************************************

    protected override void
    BackgroundWorker_DoWork
    (
        object sender,
        DoWorkEventArgs e
    )
    {
        Debug.Assert(e.Argument is GetNetworkAsyncArgs);

        GetNetworkAsyncArgs oGetNetworkAsyncArgs =
            (GetNetworkAsyncArgs)e.Argument;

        try
        {
            e.Result = GetNetworkInternal(
                oGetNetworkAsyncArgs.SearchTerm,
                oGetNetworkAsyncArgs.WhatToInclude,
                oGetNetworkAsyncArgs.MaximumStatuses
                );
        }
        catch (CancellationPendingException)
        {
            e.Cancel = true;
        }
    }


    //*************************************************************************
    //  Method: AssertValid()
    //
    /// <summary>
    /// Asserts if the object is in an invalid state.  Debug-only.
    /// </summary>
    //*************************************************************************

    // [Conditional("DEBUG")]

    public override void
    AssertValid()
    {
        base.AssertValid();

        // (Do nothing else.)
    }


    //*************************************************************************
    //  Public constants
    //*************************************************************************

    /// Maximum number of friends to request.  This is arbitrarily set to the
    /// number of friends returned in one page of the Twitter friends/ids API.
    /// It can be parameterized later if required.  

    public const Int32 MaximumFriends = 5000;


    //*************************************************************************
    //  Protected fields
    //*************************************************************************

    // (None.)


    //*************************************************************************
    //  Embedded class: GetNetworkAsyncArgs()
    //
    /// <summary>
    /// Contains the arguments needed to asynchronously get the network.
    /// </summary>
    //*************************************************************************

    protected class GetNetworkAsyncArgs : Object
    {
        ///
        public String SearchTerm;
        ///
        public WhatToInclude WhatToInclude;
        ///
        public Int32 MaximumStatuses;
    };
}

}
