require 'gtk2'

module Cpg
	class MessageArea < Gtk::HBox
		type_register
		
		attr_reader :action_area
		
		def initialize
			super({'homogeneous' => false, 'spacing' => 0})
			
			self.app_paintable = true
			
			@mainbox = Gtk::HBox.new(false, 16)
			@mainbox.border_width = 8

			@contents = nil
			@action_area = Gtk::VBox.new(TRUE, 10)
			
			@mainbox.pack_end(@action_area, false, true, 0)
			self.pack_start(@mainbox, true, true, 0)

			@changing_style = false
			
			@mainbox.signal_connect('style_set') { |w, prevstyle| do_style_set(prevstyle) }
			signal_connect('expose_event') { |w, ev| paint_message_area(ev) }
		end
		
		def contents=(c)
			@contents.destroy if @contents
			@contents = c
			
			@mainbox.pack_start(c, true, true, 0)
			c.show
		end
		
		def actions=(actions)
			@action_area.children.each { |x| x.destroy }
			
			actions.each do |action|
				@action_area.pack_start(action[0], false, false, 0)
				action[0].show

				if action[0].is_a?(Gtk::Button)
					action[0].signal_connect('clicked') { |b| action[1].call }
				end
			end
		end
		
		def do_style_set(prevstyle)
			return if @changing_style

			w = Gtk::Window.new(Gtk::Window::POPUP);
			w.name = "gtk-tooltip"
			
			w.ensure_style

			@changing_style = true
			self.set_style(w.style)
			@changing_style = false
			
			w.destroy
			queue_draw
		end
		
		def paint_message_area(event)
			self.style.paint_flat_box(self.window,
									  Gtk::STATE_NORMAL,
									  Gtk::SHADOW_OUT,
									  nil,
									  self,
									  "tooltip",
									  self.allocation.x + 1,
									  self.allocation.y + 1,
									  self.allocation.width - 2,
									  self.allocation.height - 2)
			false
		end
	end
end
