#include <gtk/gtk.h>
#include "maku_window.h"

GtkWidget *maku_make_card(const char *title, const char *subtitle, GtkWidget *control) {
    GtkWidget *card = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 12);
    gtk_widget_add_css_class(card, "maku-card");

    GtkWidget *text_box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 2);
    gtk_widget_set_hexpand(text_box, TRUE);

    GtkWidget *lbl_title = gtk_label_new(title);
    gtk_widget_add_css_class(lbl_title, "maku-card-title");
    gtk_label_set_xalign(GTK_LABEL(lbl_title), 0.0f);
    gtk_box_append(GTK_BOX(text_box), lbl_title);

    if (subtitle) {
        GtkWidget *lbl_sub = gtk_label_new(subtitle);
        gtk_widget_add_css_class(lbl_sub, "maku-card-subtitle");
        gtk_label_set_xalign(GTK_LABEL(lbl_sub), 0.0f);
        gtk_label_set_wrap(GTK_LABEL(lbl_sub), TRUE);
        gtk_box_append(GTK_BOX(text_box), lbl_sub);
    }

    gtk_box_append(GTK_BOX(card), text_box);

    if (control) {
        gtk_widget_set_valign(control, GTK_ALIGN_CENTER);
        gtk_box_append(GTK_BOX(card), control);
    }

    return card;
}
