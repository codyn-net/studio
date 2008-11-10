module Cpg
	module SignalRegistry
		def initialize(*args)
			super
		
			@signal_registry = {}
		end
	
		def connect(obj, sign, how = :signal_connect)
			@signal_registry[obj] ||= []
			@signal_registry[obj] << obj.send(how, sign) { |*args| yield *args }
		end
	
		def disconnect(obj)
			if !obj.destroyed?
				(@signal_registry[obj] || []).each { |s| obj.signal_handler_disconnect(s) }
			end
			
			@signal_registry.delete(obj)
		end
	
		def cleanup_signals
			@signal_registry.each do |obj, signals|
				disconnect(obj)
			end
		
			@signal_registry.clear
		end
	end
end
