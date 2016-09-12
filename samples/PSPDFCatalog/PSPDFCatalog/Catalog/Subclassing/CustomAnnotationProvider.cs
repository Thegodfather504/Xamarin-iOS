﻿using System;
using System.Timers;
using System.Collections.Generic;
using Foundation;
using UIKit;
using CoreGraphics;

using PSPDFKit.iOS;

namespace PSPDFCatalog
{
	public class CustomAnnotationProvider : NSObject, IPSPDFAnnotationProvider
	{
		Timer timer;
		Dictionary<nuint, PSPDFAnnotation []> annotations;
		PSPDFDocument document;
		static readonly Random rnd = new Random ();

		// MUST HAVE ctor when Subclassing!!! It will crash otherwise.
		public CustomAnnotationProvider (IntPtr handle) : base (handle)
		{
		}

		public CustomAnnotationProvider (PSPDFDocument doc)
		{
			document = doc;
			timer = new Timer (1000);
			timer.Elapsed += PickColor;
			timer.Start ();
		}

		// You must mannually Export OPTIONAL Messages/Properties from the PSPDFAnnotationProvider Protocol (aka IPSPDFAnnotationProvider)
		IPSPDFAnnotationProviderChangeNotifier providerDelegate;
		public IPSPDFAnnotationProviderChangeNotifier ProviderDelegate {
			[Export ("providerDelegate")]
			get {
				return providerDelegate;
			}
			[Export ("setProviderDelegate:")]
			set {
				if (value != providerDelegate) {
					providerDelegate = value;

					if (providerDelegate == null) {
						timer.Stop ();
						timer.Dispose ();
						timer = null;
					}
				}
			}
		}

		public PSPDFAnnotation [] AnnotationsForPage (nuint page)
		{
			if (annotations == null)
				annotations = new Dictionary<nuint, PSPDFAnnotation []> ((int)document.PageCount);

			if (annotations.ContainsKey (page))
				return annotations [page];

			// it's important that this method is:
			// - fast
			// - thread safe
			// - and caches annotations (don't always create new objects!)
			lock (this) {
				// create new note annotation and add it to the dict.
				var documentProvider = ProviderDelegate.ParentDocumentProvider;
				var pageInfo = documentProvider.Document.GetPageInfo (page);
				var noteAnnotation = new PSPDFNoteAnnotation {
					Page = page,
					DocumentProvider = documentProvider,
					Contents = string.Format ("Annotation from the custom annotationProvider for page {0}.", page + 1),
					// place it top left (PDF coordinate space starts from bottom left)
					BoundingBox = new CGRect (100, pageInfo.RotatedRect.Size.Height - 100, 32, 32),
					Editable = false
				};
				annotations.Add (page, new PSPDFAnnotation [] { noteAnnotation });
			}
			return annotations [page];
		}

		void PickColor (object o, EventArgs e)
		{
			// Random Color
			UIColor color = UIColor.FromRGBA (rnd.Next (0, 255), rnd.Next (0, 255), rnd.Next (0, 255), 1);
			lock (this) {
				foreach (var annotation in annotations) {
					if (annotation.Value != null) {
						annotation.Value [0].Color = color;

						ProviderDelegate.UpdateAnnotations (annotation.Value, true);
					}
				}
			}
		}
	}
}
